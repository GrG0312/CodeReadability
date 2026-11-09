using CodeReadabilityLib.Storage.Tracking;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Reporting.MetricResults
{
    /// <summary>
    /// The file written by <c>compute-metrics</c>: every metric value of every snippet of a
    /// dataset.
    /// </summary>
    public sealed record MetricsReport
    {
        [JsonPropertyName("generatedAtUtc")]
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// The dataset ID and input files this file was made from.
        /// </summary>
        [JsonPropertyName("provenance")]
        public required Provenance Provenance { get; init; }

        /// <summary>
        /// The llama.cpp build the grading models ran on; absent for <c>algo-only</c>.
        /// </summary>
        [JsonPropertyName("llamaCppVersion")]
        public string? LlamaCppVersion { get; set; }

        [JsonPropertyName("datasetDescription")]
        public required string DatasetDescription { get; init; }

        [JsonPropertyName("totalSnippets")]
        public required int TotalSnippets { get; init; }

        /// <summary>
        /// Which metric kinds the file contains, one of the values on
        /// <see cref="MetricKinds"/>. Later steps read this instead of inspecting the values.
        /// </summary>
        [JsonPropertyName("metricKinds")]
        public required string MetricKinds { get; init; }

        /// <summary>
        /// The algorithmic metrics computed, empty for <c>llm-only</c>.
        /// </summary>
        [JsonPropertyName("algorithmicMetrics")]
        public List<string> AlgorithmicMetrics { get; init; } = new List<string>();

        /// <summary>
        /// The LLM-based metrics graded, empty for <c>algo-only</c>.
        /// </summary>
        [JsonPropertyName("llmMetrics")]
        public List<string> LlmMetrics { get; init; } = new List<string>();

        /// <summary>
        /// The model files that graded the LLM-based metrics, without <c>.gguf</c>.
        /// </summary>
        [JsonPropertyName("graders")]
        public List<string> Graders { get; init; } = new List<string>();

        [JsonPropertyName("snippets")]
        public List<SnippetMetrics> Snippets { get; init; } = new List<SnippetMetrics>();

        /// <summary>
        /// Snippets a model could not grade. Their values are absent from
        /// <see cref="SnippetMetrics.Llm"/>.
        /// </summary>
        [JsonPropertyName("failures")]
        public List<MetricGradingFailure> Failures { get; init; } = new List<MetricGradingFailure>();
    }
}
