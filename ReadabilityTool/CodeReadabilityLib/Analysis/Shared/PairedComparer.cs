using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.Shared
{
    /// <summary>
    /// Tests whether one configuration agrees with the human scores better than another,
    /// using the snippets both were evaluated on.
    /// </summary>
    internal sealed class PairedComparer
    {
        private readonly IReadOnlyDictionary<string, double> _reference;
        private readonly AnalysisOptions _options;

        public PairedComparer(IReadOnlyDictionary<string, double> reference, AnalysisOptions options)
        {
            _reference = reference;
            _options = options;
        }

        /// <summary>
        /// Compares configuration A with configuration B.
        /// </summary>
        ///
        /// <param name="pairs">
        /// Matching runs of A and B, one pair per prompting strategy or per model. With several pairs the
        /// compared quantity is the mean Spearman over the pairs, so the verdict covers every
        /// strategy at once.
        /// </param>
        ///
        /// <returns>
        /// The comparison without its Holm adjustment, which depends on the whole family and
        /// is applied by <see cref="AdjustFamily"/>.
        /// </returns>
        public PairwiseComparison Compare(
            IReadOnlyList<(AnalysedRun A, AnalysedRun B)> pairs,
            string labelA,
            string labelB,
            string context)
        {
            // Only snippets every run answered, so that all configurations are measured on
            // exactly the same material.
            string[] ids = _reference.Keys
                .Where(id => pairs.All(p => p.A.Predictions.ContainsKey(id) && p.B.Predictions.ContainsKey(id)))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            PairwiseComparison comparison = new PairwiseComparison
            {
                Context = context,
                LabelA = labelA,
                LabelB = labelB,
                RunsA = pairs.Select(p => p.A.RunName).ToList(),
                RunsB = pairs.Select(p => p.B.RunName).ToList(),
                PairedSnippets = ids.Length
            };

            int n = ids.Length;
            if (n < 3)
            {
                return comparison with { Conclusion = $"{context}: too few snippets answered by every run to compare." };
            }

            double[] reference = ids.Select(id => _reference[id]).ToArray();
            double[][] predictionsA = pairs.Select(p => ids.Select(id => p.A.Predictions[id]).ToArray()).ToArray();
            double[][] predictionsB = pairs.Select(p => ids.Select(id => p.B.Predictions[id]).ToArray()).ToArray();

            double spearmanA = MeanSpearman(reference, predictionsA);
            double spearmanB = MeanSpearman(reference, predictionsB);

            // Resample snippets jointly for every run, so that the dependence between two
            // models judging the same snippet is preserved in the null distribution.
            double[] resampledReference = new double[n];
            double[][] resampledA = predictionsA.Select(_ => new double[n]).ToArray();
            double[][] resampledB = predictionsB.Select(_ => new double[n]).ToArray();

            double[] differences = Bootstrap.Resample(
                n,
                _options.BootstrapIterations,
                1,
                indices =>
                {
                    for (int i = 0; i < n; i++)
                    {
                        resampledReference[i] = reference[indices[i]];
                        for (int p = 0; p < pairs.Count; p++)
                        {
                            resampledA[p][i] = predictionsA[p][indices[i]];
                            resampledB[p][i] = predictionsB[p][indices[i]];
                        }
                    }

                    return [MeanSpearman(resampledReference, resampledB) - MeanSpearman(resampledReference, resampledA)];
                },
                Bootstrap.DeriveSeed(_options.Seed, string.Join('|', comparison.RunsA) + "||" + string.Join('|', comparison.RunsB)))[0];

            ConfidenceInterval? interval = Bootstrap.PercentileInterval(differences, _options.Alpha);

            // Per-snippet absolute error, averaged over strategies; positive difference means
            // B errs more on that snippet.
            double[] errorsA = MeanAbsoluteErrors(reference, predictionsA);
            double[] errorsB = MeanAbsoluteErrors(reference, predictionsB);
            WilcoxonResult wilcoxon = HypothesisTests.WilcoxonSignedRank(
                errorsB.Zip(errorsA, (b, a) => b - a).ToArray());

            return comparison with
            {
                SpearmanA = AnalysisFormat.Finite(spearmanA),
                SpearmanB = AnalysisFormat.Finite(spearmanB),
                SpearmanDifference = AnalysisFormat.Finite(spearmanB - spearmanA),
                DifferenceInterval = interval is ConfidenceInterval ci ? new Interval { Lower = ci.Lower, Upper = ci.Upper } : null,
                PValue = AnalysisFormat.Finite(Bootstrap.TwoSidedPValue(differences)),
                MaeA = AnalysisFormat.Finite(errorsA.Average()),
                MaeB = AnalysisFormat.Finite(errorsB.Average()),
                MaePValue = AnalysisFormat.Finite(wilcoxon.PValue)
            };
        }

        /// <summary>
        /// Applies the Holm correction across a family of comparisons and writes the
        /// resulting verdicts.
        /// </summary>
        public List<PairwiseComparison> AdjustFamily(IReadOnlyList<PairwiseComparison> family)
        {
            double[] adjusted = HypothesisTests.HolmAdjust(family.Select(c => c.PValue ?? double.NaN).ToArray());
            double[] maeAdjusted = HypothesisTests.HolmAdjust(family.Select(c => c.MaePValue ?? double.NaN).ToArray());

            List<PairwiseComparison> result = new List<PairwiseComparison>();

            for (int i = 0; i < family.Count; i++)
            {
                PairwiseComparison comparison = family[i] with
                {
                    AdjustedPValue = AnalysisFormat.Finite(adjusted[i]),
                    MaeAdjustedPValue = AnalysisFormat.Finite(maeAdjusted[i])
                };

                if (comparison.SpearmanDifference is not double difference || comparison.AdjustedPValue is not double p)
                {
                    result.Add(comparison);
                    continue;
                }

                bool significant = p < _options.Alpha && difference != 0;
                string? better = significant ? (difference > 0 ? comparison.LabelB : comparison.LabelA) : null;

                result.Add(comparison with
                {
                    Significant = significant,
                    Better = better,
                    Conclusion = Describe(comparison, significant, difference, p)
                });
            }

            return result;
        }

        private string Describe(PairwiseComparison c, bool significant, double difference, double adjustedP)
        {
            string interval = c.DifferenceInterval is Interval ci
                ? $", {AnalysisFormat.Format((1 - _options.Alpha) * 100, "0.#")}% CI [{AnalysisFormat.Format(ci.Lower)}, {AnalysisFormat.Format(ci.Upper)}]"
                : string.Empty;

            string evidence = $"(Δρ ({c.LabelB} − {c.LabelA}) = {AnalysisFormat.Format(difference, "+0.000;-0.000")}{interval}, Holm p = {AnalysisFormat.FormatP(adjustedP)}; " +
                              $"ρ {AnalysisFormat.Format(c.SpearmanA ?? double.NaN)} vs {AnalysisFormat.Format(c.SpearmanB ?? double.NaN)}, n = {c.PairedSnippets})";

            if (!significant)
            {
                return $"{c.Context}: no significant difference between {c.LabelA} and {c.LabelB} {evidence}.";
            }

            (string winner, string loser) = difference > 0 ? (c.LabelB, c.LabelA) : (c.LabelA, c.LabelB);
            return $"{c.Context}: {winner} agrees with the human scores significantly better than {loser} {evidence}.";
        }

        private static double MeanSpearman(double[] reference, double[][] predictions)
        {
            double sum = 0;
            foreach (double[] prediction in predictions)
            {
                sum += Correlation.Spearman(reference, prediction);
            }

            return sum / predictions.Length;
        }

        private static double[] MeanAbsoluteErrors(double[] reference, double[][] predictions)
        {
            double[] errors = new double[reference.Length];
            for (int i = 0; i < reference.Length; i++)
            {
                foreach (double[] prediction in predictions)
                {
                    errors[i] += Math.Abs(prediction[i] - reference[i]);
                }

                errors[i] /= predictions.Length;
            }

            return errors;
        }
    }
}
