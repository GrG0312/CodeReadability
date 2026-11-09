using CodeReadabilityLib.Llms.Judging.Prompting;
using CodeReadabilityLib.Statistics;
using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Analysis.Shared;

namespace CodeReadabilityLib.Analysis.Prompting
{
    /// <summary>
    /// Determines which prompting strategy works best overall and per model, and whether
    /// changing strategy genuinely changes a model's agreement with the humans.
    /// </summary>
    internal sealed class PromptingStrategyAnalyzer
    {
        private readonly PairedComparer _comparer;
        private readonly AnalysisOptions _options;
        private readonly IReadOnlyDictionary<string, double?> _spearman;

        /// <param name="spearmanByRun">
        /// Each run's Spearman against the human scores, keyed by run name.
        /// </param>
        public PromptingStrategyAnalyzer(PairedComparer comparer, AnalysisOptions options, IReadOnlyDictionary<string, double?> spearmanByRun)
        {
            _comparer = comparer;
            _options = options;
            _spearman = spearmanByRun;
        }

        /// <returns>
        /// The analysis, or <see langword="null"/> when fewer than two strategies were run.
        /// </returns>
        public PromptingStrategyReport? Analyze(IReadOnlyList<AnalysedRun> runs)
        {
            List<string> strategies = OrderStrategies(runs.Select(r => r.Strategy).Distinct());
            if (strategies.Count < 2)
            {
                return null;
            }

            List<ModelStrategyReport> perModel = runs
                .GroupBy(r => r.ModelName)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => AnalyzeModel(g.Key, g.ToList(), strategies))
                .ToList();

            // Friedman needs complete blocks: models run with every strategy.
            List<ModelStrategyReport> complete = perModel
                .Where(m => strategies.All(s => m.SpearmanByStrategy.TryGetValue(s, out double? rho) && rho.HasValue))
                .ToList();

            FriedmanResult friedman = HypothesisTests.Friedman(
                complete.Select(m => strategies.Select(s => m.SpearmanByStrategy[s]!.Value).ToArray()).ToList());

            List<StrategyOverall> overall = strategies
                .Select((strategy, index) =>
                {
                    double?[] values = perModel
                        .Select(m => m.SpearmanByStrategy.GetValueOrDefault(strategy))
                        .Where(v => v.HasValue)
                        .ToArray();

                    return new StrategyOverall
                    {
                        Strategy = strategy,
                        Description = PromptingStrategies.All.FirstOrDefault(s => s.Id == strategy)?.Description,
                        MeanSpearman = AnalysisFormat.Finite(Correlation.FisherMean(values.Select(v => v!.Value))),
                        MeanRank = complete.Count > 0 ? AnalysisFormat.Finite(friedman.MeanRanks[index]) : null,
                        Wins = perModel.Count(m => m.BestStrategy == strategy),
                        Models = values.Length
                    };
                })
                .OrderBy(s => s.MeanRank ?? double.PositiveInfinity)
                .ThenByDescending(s => s.MeanSpearman ?? double.NegativeInfinity)
                .ToList();

            FriedmanSummary summary = new FriedmanSummary
            {
                ChiSquared = AnalysisFormat.Finite(friedman.ChiSquared),
                DegreesOfFreedom = friedman.DegreesOfFreedom,
                PValue = AnalysisFormat.Finite(friedman.PValue),
                Models = friedman.Blocks,
                Significant = friedman.PValue < _options.Alpha
            };

            string? best = overall.FirstOrDefault()?.Strategy;

            return new PromptingStrategyReport
            {
                Strategies = overall,
                BestOverall = best,
                Friedman = summary,
                OverallPairwise = CompareAcrossModels(runs, strategies),
                PerModel = perModel,
                Conclusion = DescribeOverall(best, overall.FirstOrDefault(), summary)
            };
        }

        /// <summary>
        /// Compares every pair of strategies across models: the mean Spearman over the
        /// models run with both, resampling snippets jointly for every model.
        /// </summary>
        ///
        /// <remarks>
        /// Holm-corrected across the strategy pairs. Uses every snippet, so it is far more
        /// sensitive than the Friedman test, which sees one value per model.
        /// </remarks>
        private List<PairwiseComparison> CompareAcrossModels(IReadOnlyList<AnalysedRun> runs, List<string> strategies)
        {
            List<PairwiseComparison> comparisons = new List<PairwiseComparison>();

            for (int i = 0; i < strategies.Count; i++)
            {
                for (int j = i + 1; j < strategies.Count; j++)
                {
                    List<(AnalysedRun A, AnalysedRun B)> pairs = runs
                        .Where(r => r.Strategy == strategies[i])
                        .Join(runs.Where(r => r.Strategy == strategies[j]), r => r.ModelName, r => r.ModelName, (a, b) => (a, b))
                        .OrderBy(p => p.a.ModelName, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (pairs.Count > 0)
                    {
                        comparisons.Add(_comparer.Compare(pairs, strategies[i], strategies[j], $"all models, pooled over {pairs.Count} model(s)"));
                    }
                }
            }

            return _comparer.AdjustFamily(comparisons);
        }

        private ModelStrategyReport AnalyzeModel(string modelName, List<AnalysedRun> runs, List<string> strategies)
        {
            Dictionary<string, double?> spearman = strategies.ToDictionary(
                s => s,
                s => runs.FirstOrDefault(r => r.Strategy == s) is AnalysedRun run ? _spearman.GetValueOrDefault(run.RunName) : null);

            string? best = spearman
                .Where(kv => kv.Value.HasValue)
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .FirstOrDefault();

            List<AnalysedRun> ordered = strategies
                .Select(s => runs.FirstOrDefault(r => r.Strategy == s))
                .OfType<AnalysedRun>()
                .ToList();

            List<PairwiseComparison> comparisons = new List<PairwiseComparison>();
            for (int i = 0; i < ordered.Count; i++)
            {
                for (int j = i + 1; j < ordered.Count; j++)
                {
                    comparisons.Add(_comparer.Compare([(ordered[i], ordered[j])], ordered[i].Strategy, ordered[j].Strategy, modelName));
                }
            }

            // Holm within the model: the question asked per model is whether any strategy
            // change mattered for it.
            List<PairwiseComparison> adjusted = _comparer.AdjustFamily(comparisons);
            bool impact = adjusted.Any(c => c.Significant);

            return new ModelStrategyReport
            {
                ModelName = modelName,
                SpearmanByStrategy = spearman,
                BestStrategy = best,
                Comparisons = adjusted,
                GenuineImpact = impact,
                Conclusion = DescribeModel(modelName, best, spearman, adjusted, impact)
            };
        }

        private static string DescribeModel(
            string modelName,
            string? best,
            Dictionary<string, double?> spearman,
            List<PairwiseComparison> comparisons,
            bool impact)
        {
            if (best is null)
            {
                return $"{modelName}: no strategy produced a usable Spearman correlation.";
            }

            string head = $"{modelName}: best strategy is {best} (ρ = {AnalysisFormat.Format(spearman[best]!.Value)}).";

            if (!impact)
            {
                return head + " No pair of strategies differs significantly, so the choice of prompt did not measurably change this model's agreement.";
            }

            IEnumerable<string> wins = comparisons
                .Where(c => c.Significant)
                .Select(c => $"{c.Better} > {(c.Better == c.LabelA ? c.LabelB : c.LabelA)}");

            return head + " The prompt genuinely matters for this model: " + string.Join(", ", wins) + ".";
        }

        private string DescribeOverall(string? best, StrategyOverall? top, FriedmanSummary friedman)
        {
            if (best is null || top is null)
            {
                return "No strategy produced usable results.";
            }

            string ranking = top.MeanRank is double rank
                ? $"mean rank {AnalysisFormat.Format(rank, "0.00")}, mean ρ = {AnalysisFormat.Format(top.MeanSpearman ?? double.NaN)}, best for {top.Wins} model(s)"
                : $"mean ρ = {AnalysisFormat.Format(top.MeanSpearman ?? double.NaN)}";

            if (friedman.PValue is not double p)
            {
                return $"Best overall: {best} ({ranking}). Too few models were run with every strategy to test whether strategies differ overall.";
            }

            string test = $"Friedman χ²({friedman.DegreesOfFreedom}) = {AnalysisFormat.Format(friedman.ChiSquared ?? double.NaN, "0.00")}, p = {AnalysisFormat.FormatP(p)}, {friedman.Models} models";

            return friedman.Significant
                ? $"Prompting strategy has a significant overall effect ({test}). Best overall: {best} ({ranking})."
                : $"{best} ranks best overall ({ranking}), but across models the strategies do not differ significantly ({test}).";
        }

        /// <summary>
        /// Registered strategies in registry order (baseline first), then any unknown ids
        /// from older or hand-edited reports alphabetically.
        /// </summary>
        private static List<string> OrderStrategies(IEnumerable<string> ids)
        {
            List<string> known = PromptingStrategies.All.Select(s => s.Id).ToList();

            return ids
                .OrderBy(id => known.IndexOf(id) is int index && index >= 0 ? index : int.MaxValue)
                .ThenBy(id => id, StringComparer.Ordinal)
                .ToList();
        }
    }
}
