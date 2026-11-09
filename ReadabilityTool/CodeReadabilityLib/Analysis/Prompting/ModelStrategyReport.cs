using CodeReadabilityLib.Analysis.Shared;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Prompting
{
    /// <summary>
    /// How the prompting strategies compare for one model.
    /// </summary>
    public sealed record ModelStrategyReport
    {
        [JsonPropertyName("modelName")]
        public required string ModelName { get; init; }

        /// <summary>
        /// Spearman of each strategy for this model.
        /// </summary>
        [JsonPropertyName("spearmanByStrategy")]
        public Dictionary<string, double?> SpearmanByStrategy { get; init; } = new Dictionary<string, double?>();

        [JsonPropertyName("bestStrategy")]
        public string? BestStrategy { get; init; }

        /// <summary>
        /// Every pair of strategies compared on this model, Holm-corrected within the model.
        /// </summary>
        [JsonPropertyName("comparisons")]
        public List<PairwiseComparison> Comparisons { get; init; } = new List<PairwiseComparison>();

        /// <summary>
        /// Whether any pair of strategies differed significantly, meaning the choice of
        /// prompt genuinely changed how well this model predicts the human scores.
        /// </summary>
        [JsonPropertyName("genuineImpact")]
        public bool GenuineImpact { get; init; }

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; init; } = string.Empty;
    }
}
