using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Reporting.MetricResults
{
    /// <summary>
    /// A snippet a model could not grade, even after retries.
    /// </summary>
    public sealed record MetricGradingFailure
    {
        [JsonPropertyName("snippetId")]
        public required string SnippetId { get; init; }

        /// <summary>
        /// The model file that failed, without <c>.gguf</c>.
        /// </summary>
        [JsonPropertyName("modelFile")]
        public required string ModelFile { get; init; }

        [JsonPropertyName("reason")]
        public required string Reason { get; init; }
    }
}
