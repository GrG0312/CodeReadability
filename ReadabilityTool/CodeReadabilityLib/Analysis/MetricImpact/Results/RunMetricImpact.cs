using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// The metric impact of one run (one model, one prompting strategy).
    /// </summary>
    public sealed record RunMetricImpact
    {
        [JsonPropertyName("modelName")]
        public required string ModelName { get; init; }

        [JsonPropertyName("promptingStrategy")]
        public required string PromptingStrategy { get; init; }

        [JsonPropertyName("ranking")]
        public required MetricRanking Ranking { get; init; }

        [JsonPropertyName("versusHuman")]
        public List<ModelVersusHuman> VersusHuman { get; init; } = new List<ModelVersusHuman>();
    }
}
