using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// How far the grader models agree on one LLM-based metric.
    /// </summary>
    public sealed record GraderAgreement
    {
        [JsonPropertyName("metricId")]
        public required string MetricId { get; init; }

        /// <summary>
        /// ICC(3,k): consistency of the averaged grades.
        /// </summary>
        [JsonPropertyName("icc")]
        public double? Icc { get; init; }

        /// <summary>
        /// poor (&lt; 0.50), moderate (0.50–0.75), good (0.75–0.90) or excellent (&gt; 0.90).
        /// </summary>
        [JsonPropertyName("band")]
        public required string Band { get; init; }

        [JsonPropertyName("graders")]
        public int Graders { get; init; }

        /// <summary>
        /// Snippets graded by every grader, the only ones the ICC uses.
        /// </summary>
        [JsonPropertyName("snippets")]
        public int Snippets { get; init; }

        /// <summary>
        /// Set when agreement is poor. The metric stays in every analysis but is marked.
        /// </summary>
        [JsonPropertyName("flagged")]
        public bool Flagged { get; init; }
    }
}
