using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Values
{
    /// <summary>
    /// A metric taking part in the metric-impact analysis.
    /// </summary>
    public sealed record MetricInfo
    {
        [JsonPropertyName("metricId")]
        public required string MetricId { get; init; }

        /// <summary>
        /// <c>algorithmic</c> or <c>llm</c>.
        /// </summary>
        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        [JsonIgnore]
        public bool IsLlm => Kind == LlmKind;

        public const string AlgorithmicKind = "algorithmic";

        public const string LlmKind = "llm";
    }
}
