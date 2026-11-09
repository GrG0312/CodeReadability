using CodeReadabilityLib.Analysis.MetricImpact.Values;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// Which metrics the human and model readability scores follow, how strongly, and where
    /// models and humans differ.
    /// </summary>
    public sealed record MetricImpactReport
    {
        /// <summary>
        /// The metric kinds of the metrics file: <c>algo-only</c>, <c>llm-only</c> or
        /// <c>both</c>.
        /// </summary>
        [JsonPropertyName("metricKinds")]
        public required string MetricKinds { get; init; }

        [JsonPropertyName("metrics")]
        public List<MetricInfo> Metrics { get; init; } = new List<MetricInfo>();

        /// <summary>
        /// Snippets with a human score and metric values, the material of every correlation.
        /// </summary>
        [JsonPropertyName("snippets")]
        public int Snippets { get; init; }

        /// <summary>
        /// The metrics ranked by how strongly the human scores follow them.
        /// </summary>
        [JsonPropertyName("human")]
        public required MetricRanking Human { get; init; }

        /// <summary>
        /// The metrics ranked by how strongly the models' scores follow them, each model's
        /// correlation pooled over models through the Fisher z-transform.
        /// </summary>
        [JsonPropertyName("overall")]
        public required MetricRanking Overall { get; init; }

        [JsonPropertyName("perModel")]
        public List<ModelMetricImpact> PerModel { get; init; } = new List<ModelMetricImpact>();

        [JsonPropertyName("perRun")]
        public List<RunMetricImpact> PerRun { get; init; } = new List<RunMetricImpact>();

        /// <summary>
        /// One entry per LLM-based metric. Empty without LLM-based metrics.
        /// </summary>
        [JsonPropertyName("graderAgreement")]
        public List<GraderAgreement> GraderAgreement { get; init; } = new List<GraderAgreement>();

        [JsonPropertyName("notes")]
        public List<string> Notes { get; init; } = new List<string>();
    }
}
