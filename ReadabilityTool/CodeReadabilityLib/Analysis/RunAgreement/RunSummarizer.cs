using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.RunAgreement
{
    /// <summary>
    /// Computes every run's agreement statistics with bootstrap confidence intervals.
    /// </summary>
    internal sealed class RunSummarizer
    {
        private readonly AnalysisOptions _options;

        public RunSummarizer(AnalysisOptions options)
        {
            _options = options;
        }

        /// <returns>
        /// One summary per run, best first: by Spearman, then kappa, then error.
        /// </returns>
        public List<RunSummary> Summarise(IReadOnlyList<AnalysedRun> runs, IReadOnlyDictionary<string, double> reference, double threshold)
        {
            RunSummary[] summaries = new RunSummary[runs.Count];

            // Each run has its own seed, so the result does not depend on scheduling order.
            Parallel.For(0, runs.Count, index =>
            {
                AnalysedRun run = runs[index];
                summaries[index] = new RunSummary
                {
                    ModelName = run.ModelName,
                    PromptingStrategy = run.Strategy,
                    DistanceHistogram = AgreementCalculator.DistanceHistogram(run.Run),
                    Statistics = AgreementCalculator.Compute(run.Run, threshold),
                    ConfidenceIntervals = Intervals(run, reference, threshold)
                };
            });

            // Rank correlation leads because ordering by exact matches rewards a model that
            // always guesses the most common score.
            return summaries
                .OrderByDescending(s => s.Statistics.Spearman ?? double.NegativeInfinity)
                .ThenByDescending(s => s.Statistics.QuadraticWeightedKappa ?? double.NegativeInfinity)
                .ThenBy(s => s.Statistics.MeanAbsoluteError ?? double.PositiveInfinity)
                .ToList();
        }

        private Dictionary<string, Interval> Intervals(AnalysedRun run, IReadOnlyDictionary<string, double> reference, double threshold)
        {
            Dictionary<string, Interval> intervals = new Dictionary<string, Interval>();

            if (AnswerRateInterval(run) is Interval answerRate)
            {
                intervals[AnswerRateKey] = answerRate;
            }

            string[] ids = run.Predictions.Keys.Where(reference.ContainsKey).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            if (ids.Length < 3)
            {
                return intervals;
            }

            double[] human = ids.Select(id => reference[id]).ToArray();
            double[] model = ids.Select(id => run.Predictions[id]).ToArray();
            double[] resampledHuman = new double[ids.Length];
            double[] resampledModel = new double[ids.Length];

            double[][] distributions = Bootstrap.Resample(
                ids.Length,
                _options.BootstrapIterations,
                AgreementSnapshot.Count,
                indices =>
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        resampledHuman[i] = human[indices[i]];
                        resampledModel[i] = model[indices[i]];
                    }

                    return AgreementSnapshot
                        .Compute(resampledHuman, resampledModel, ReadabilityScale.Min, ReadabilityScale.Max, threshold)
                        .ToArray();
                },
                Bootstrap.DeriveSeed(_options.Seed, run.RunName));

            foreach (StatisticDefinition statistic in StatisticCatalog.All)
            {
                if (Bootstrap.PercentileInterval(distributions[statistic.Index], _options.Alpha) is ConfidenceInterval ci)
                {
                    intervals[statistic.Key] = new Interval { Lower = ci.Lower, Upper = ci.Upper };
                }
            }

            return intervals;
        }

        /// <summary>
        /// The key of the answer rate's interval in a run summary.
        /// </summary>
        public const string AnswerRateKey = "answerRate";

        /// <summary>
        /// The bootstrap interval of the share of snippets answered, resampling every snippet
        /// the run was asked about, answered or not.
        /// </summary>
        private Interval? AnswerRateInterval(AnalysedRun run)
        {
            double[] answered = run.Run.ConcreteAnswers.Select(answer => answer.ModelAnswer is null ? 0d : 1d).ToArray();
            if (answered.Length == 0)
            {
                return null;
            }

            double[][] distribution = Bootstrap.Resample(
                answered.Length,
                _options.BootstrapIterations,
                1,
                indices => [indices.Average(i => answered[i])],
                Bootstrap.DeriveSeed(_options.Seed, run.RunName + "|" + AnswerRateKey));

            return Bootstrap.PercentileInterval(distribution[0], _options.Alpha) is ConfidenceInterval ci
                ? new Interval { Lower = ci.Lower, Upper = ci.Upper }
                : null;
        }
    }
}
