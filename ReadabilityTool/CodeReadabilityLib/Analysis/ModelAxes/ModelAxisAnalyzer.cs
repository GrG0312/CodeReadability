using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Llms.Models.Descriptions;

namespace CodeReadabilityLib.Analysis.ModelAxes
{
    /// <summary>
    /// Compares models that differ in exactly one characteristic: quantization, size,
    /// reasoning or family.
    /// </summary>
    ///
    /// <remarks>
    /// Two models are compared on an axis only when every other characteristic is identical,
    /// so that a difference in agreement can be attributed to that axis alone. The
    /// characteristics come from the hand-filled model descriptions and are compared ignoring
    /// upper/lower case and surrounding spaces.
    /// </remarks>
    internal sealed class ModelAxisAnalyzer
    {
        private static readonly AxisDefinition[] Axes =
        [
            new AxisDefinition
            {
                Name = "quantization",
                Description = "Same family, size and type at different quantizations; B is the higher-precision one.",
                Value = d => d.Quant,
                Order = d => AxisOrdering.Bits(d.Quant),
                Fixed = d => [d.Family, d.Size, d.Type],
                HigherLabel = "the higher-precision model",
                LowerLabel = "the lower-precision model"
            },
            new AxisDefinition
            {
                Name = "size",
                Description = "Same family, quantization and type at different sizes; B is the larger one.",
                Value = d => d.Size,
                Order = d => AxisOrdering.SizeInBillions(d.Size),
                Fixed = d => [d.Family, d.Quant, d.Type],
                HigherLabel = "the larger model",
                LowerLabel = "the smaller model"
            },
            new AxisDefinition
            {
                Name = "reasoning",
                Description = "Same family, size and quantization as flash and reasoning; B is the reasoning one.",
                Value = d => d.Type,
                Order = d => ModelTypes.Normalize(d.Type) == ModelTypes.Reasoning ? 1 : 0,
                Fixed = d => [d.Family, d.Size, d.Quant],
                HigherLabel = "the reasoning model",
                LowerLabel = "the flash model"
            },
            new AxisDefinition
            {
                Name = "family",
                Description = "Same size, quantization and type from different families; A and B in alphabetical order.",
                Value = d => d.Family,
                Order = d => ModelTypes.Normalize(d.Family),
                Fixed = d => [d.Size, d.Quant, d.Type],
                HigherLabel = "the alphabetically later family",
                LowerLabel = "the alphabetically earlier family"
            }
        ];

        private readonly PairedComparer _comparer;

        public ModelAxisAnalyzer(PairedComparer comparer)
        {
            _comparer = comparer;
        }

        public List<AxisComparison> Analyze(IReadOnlyList<AnalysedRun> runs)
        {
            return Axes.Select(axis => AnalyzeAxis(axis, runs)).ToList();
        }

        private AxisComparison AnalyzeAxis(AxisDefinition axis, IReadOnlyList<AnalysedRun> runs)
        {
            // One description per model; the runs of a model differ only by strategy.
            Dictionary<string, ModelDescription> models = runs
                .GroupBy(r => r.ModelName)
                .ToDictionary(g => g.Key, g => g.First().Description);

            List<(string A, string B)> modelPairs = new List<(string, string)>();

            foreach (IGrouping<string, KeyValuePair<string, ModelDescription>> group in models
                .GroupBy(model => string.Join('|', axis.Fixed(model.Value).Select(ModelTypes.Normalize))))
            {
                KeyValuePair<string, ModelDescription>[] ordered = group
                    .OrderBy(model => axis.Order(model.Value))
                    .ThenBy(model => model.Key, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                for (int i = 0; i < ordered.Length; i++)
                {
                    for (int j = i + 1; j < ordered.Length; j++)
                    {
                        if (ModelTypes.Normalize(axis.Value(ordered[i].Value)) != ModelTypes.Normalize(axis.Value(ordered[j].Value)))
                        {
                            modelPairs.Add((ordered[i].Key, ordered[j].Key));
                        }
                    }
                }
            }

            HashSet<string> matched = modelPairs.SelectMany(p => new[] { p.A, p.B }).ToHashSet();
            List<string> unmatched = models.Keys
                .Where(name => !matched.Contains(name))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<PairwiseComparison> pooled = new List<PairwiseComparison>();
            List<PairwiseComparison> perStrategy = new List<PairwiseComparison>();

            foreach ((string a, string b) in modelPairs)
            {
                // Only strategies both models were run with take part.
                List<(AnalysedRun A, AnalysedRun B)> strategyPairs = runs
                    .Where(r => r.ModelName == a)
                    .Join(runs.Where(r => r.ModelName == b), r => r.Strategy, r => r.Strategy, (ra, rb) => (ra, rb))
                    .OrderBy(p => p.ra.Strategy, StringComparer.Ordinal)
                    .ToList();

                if (strategyPairs.Count == 0)
                {
                    continue;
                }

                string labelA = $"{axis.Value(models[a])} ({a})";
                string labelB = $"{axis.Value(models[b])} ({b})";
                string fixedLabel = string.Join(' ', axis.Fixed(models[a]));
                string strategies = strategyPairs.Count == 1
                    ? strategyPairs[0].A.Strategy
                    : $"pooled over {strategyPairs.Count} strategies";

                pooled.Add(_comparer.Compare(strategyPairs, labelA, labelB, $"{fixedLabel}, {strategies}"));

                if (strategyPairs.Count > 1)
                {
                    foreach ((AnalysedRun ra, AnalysedRun rb) in strategyPairs)
                    {
                        perStrategy.Add(_comparer.Compare([(ra, rb)], labelA, labelB, $"{fixedLabel}, {ra.Strategy}"));
                    }
                }
            }

            List<PairwiseComparison> adjustedPooled = _comparer.AdjustFamily(pooled);

            return new AxisComparison
            {
                Axis = axis.Name,
                Description = axis.Description,
                Comparisons = adjustedPooled,
                PerStrategyComparisons = _comparer.AdjustFamily(perStrategy),
                UnmatchedModels = unmatched,
                Summary = Summarise(axis, adjustedPooled)
            };
        }

        private static string Summarise(AxisDefinition axis, IReadOnlyList<PairwiseComparison> comparisons)
        {
            if (comparisons.Count == 0)
            {
                return $"No pair of models differs only in {axis.Name}, so nothing could be compared on this axis.";
            }

            int favourB = comparisons.Count(c => c.Significant && c.SpearmanDifference > 0);
            int favourA = comparisons.Count(c => c.Significant && c.SpearmanDifference < 0);
            int none = comparisons.Count - favourA - favourB;

            return $"{comparisons.Count} comparison(s): {favourB} significantly favour {axis.HigherLabel}, " +
                   $"{favourA} significantly favour {axis.LowerLabel}, {none} show no significant difference.";
        }
    }
}
