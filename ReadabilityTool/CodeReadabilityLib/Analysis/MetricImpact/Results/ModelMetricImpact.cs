using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// The metric impact of one model, with its runs across all strategies pooled. The main
    /// metric-impact result.
    /// </summary>
    public sealed record ModelMetricImpact
    {
        [JsonPropertyName("modelName")]
        public required string ModelName { get; init; }

        /// <summary>
        /// The model file whose own grades were left out of the LLM-based metric values.
        /// </summary>
        [JsonPropertyName("modelFile")]
        public required string ModelFile { get; init; }

        /// <summary>
        /// The metrics ranked by how strongly the model's scores follow them; correlations
        /// are pooled over the model's runs through the Fisher z-transform.
        /// </summary>
        [JsonPropertyName("ranking")]
        public required MetricRanking Ranking { get; init; }

        /// <summary>
        /// Per metric: does the model follow it more or less than the humans do?
        /// </summary>
        [JsonPropertyName("versusHuman")]
        public List<ModelVersusHuman> VersusHuman { get; init; } = new List<ModelVersusHuman>();

        /// <summary>
        /// The "same priorities as humans" number of each of the model's runs, keyed by
        /// strategy: whether a strategy moves the model's metric profile closer to the
        /// humans'.
        /// </summary>
        [JsonPropertyName("samePrioritiesByStrategy")]
        public Dictionary<string, double?> SamePrioritiesByStrategy { get; init; } = new Dictionary<string, double?>();

        /// <summary>
        /// The strategy whose metric profile is closest to the humans'.
        /// </summary>
        [JsonPropertyName("closestStrategy")]
        public string? ClosestStrategy { get; init; }

        /// <summary>
        /// Whether the model's readability scores follow its own LLM-based metric grades.
        /// Empty when the model did not grade the metrics.
        /// </summary>
        [JsonPropertyName("selfConsistency")]
        public List<SelfConsistencyEntry> SelfConsistency { get; init; } = new List<SelfConsistencyEntry>();
    }
}
