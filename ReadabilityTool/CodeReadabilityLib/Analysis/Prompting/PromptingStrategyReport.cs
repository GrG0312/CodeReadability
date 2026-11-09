using CodeReadabilityLib.Analysis.Shared;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Prompting
{
    /// <summary>
    /// The prompting-strategy analysis: overall effect, overall pairwise comparisons, and
    /// per-model comparisons.
    /// </summary>
    public sealed record PromptingStrategyReport
    {
        [JsonPropertyName("strategies")]
        public List<StrategyOverall> Strategies { get; init; } = new List<StrategyOverall>();

        [JsonPropertyName("bestOverall")]
        public string? BestOverall { get; init; }

        /// <summary>
        /// Friedman test across models: does the choice of strategy make a difference at all?
        /// </summary>
        [JsonPropertyName("friedman")]
        public FriedmanSummary? Friedman { get; init; }

        /// <summary>
        /// Every pair of strategies compared across models by a pooled paired bootstrap of
        /// the mean Spearman over models, Holm-corrected across the strategy pairs: is
        /// strategy X better than Y overall?
        /// </summary>
        [JsonPropertyName("overallPairwise")]
        public List<PairwiseComparison> OverallPairwise { get; init; } = new List<PairwiseComparison>();

        [JsonPropertyName("perModel")]
        public List<ModelStrategyReport> PerModel { get; init; } = new List<ModelStrategyReport>();

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; init; } = string.Empty;
    }
}
