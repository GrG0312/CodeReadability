using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// How strongly a model's readability scores follow its own grade of one LLM-based
    /// metric.
    /// </summary>
    public sealed record SelfConsistencyEntry
    {
        [JsonPropertyName("metricId")]
        public required string MetricId { get; init; }

        /// <summary>
        /// Spearman between the model's own grades and its readability scores, pooled over
        /// its runs through the Fisher z-transform.
        /// </summary>
        [JsonPropertyName("spearman")]
        public double? Spearman { get; init; }

        [JsonPropertyName("runs")]
        public int Runs { get; init; }
    }
}
