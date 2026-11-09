using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Prompting
{
    /// <summary>
    /// How one prompting strategy did across all models.
    /// </summary>
    public sealed record StrategyOverall
    {
        [JsonPropertyName("strategy")]
        public required string Strategy { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        /// <summary>
        /// Spearman averaged over models through the Fisher z-transform.
        /// </summary>
        [JsonPropertyName("meanSpearman")]
        public double? MeanSpearman { get; init; }

        /// <summary>
        /// Mean rank among strategies within each model, 1 being best (the Friedman rank).
        /// </summary>
        [JsonPropertyName("meanRank")]
        public double? MeanRank { get; init; }

        /// <summary>
        /// The number of models for which this was the best strategy.
        /// </summary>
        [JsonPropertyName("wins")]
        public int Wins { get; init; }

        [JsonPropertyName("models")]
        public int Models { get; init; }
    }
}
