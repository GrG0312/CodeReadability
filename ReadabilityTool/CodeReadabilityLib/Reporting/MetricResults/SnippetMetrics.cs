using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Reporting.MetricResults
{
    /// <summary>
    /// Every metric value computed for one snippet.
    /// </summary>
    public sealed record SnippetMetrics
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        /// <summary>
        /// Raw algorithmic metric values, keyed by metric id. A metric that failed or produced
        /// a non-finite value is absent.
        /// </summary>
        [JsonPropertyName("algorithmic")]
        public Dictionary<string, double> Algorithmic { get; init; } = new Dictionary<string, double>();

        /// <summary>
        /// LLM-based metric grades, keyed by model file and then by metric id. Each model's
        /// own grades are kept separately; averaging happens in later steps.
        /// </summary>
        [JsonPropertyName("llm")]
        public Dictionary<string, Dictionary<string, double>> Llm { get; init; } =
            new Dictionary<string, Dictionary<string, double>>();
    }
}
