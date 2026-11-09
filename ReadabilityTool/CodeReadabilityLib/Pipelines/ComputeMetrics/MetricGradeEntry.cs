using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Pipelines.ComputeMetrics
{
    /// <summary>
    /// One model's grading of one snippet, as saved to the checkpoint store.
    /// </summary>
    public sealed record MetricGradeEntry
    {
        [JsonPropertyName("snippetId")]
        public required string SnippetId { get; init; }

        /// <summary>
        /// The grades keyed by metric id, or <see langword="null"/> when the model failed.
        /// </summary>
        [JsonPropertyName("grades")]
        public Dictionary<string, double>? Grades { get; init; }

        [JsonPropertyName("failureReason")]
        public string? FailureReason { get; init; }
    }
}
