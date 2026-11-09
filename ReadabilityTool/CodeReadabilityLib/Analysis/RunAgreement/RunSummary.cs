using CodeReadabilityLib.Analysis.Shared;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.RunAgreement
{
    /// <summary>
    /// One run's agreement with the human scores.
    /// </summary>
    public sealed record RunSummary
    {
        [JsonPropertyName("modelName")]
        public required string ModelName { get; init; }

        [JsonPropertyName("promptingStrategy")]
        public required string PromptingStrategy { get; init; }

        /// <summary>
        /// Usable answers bucketed by distance from the human score: index <c>i</c> counts
        /// answers off by <c>i</c> (rounded down), index 3 everything off by three or more.
        /// </summary>
        [JsonPropertyName("distanceHistogram")]
        public required int[] DistanceHistogram { get; init; }

        [JsonPropertyName("statistics")]
        public required AgreementStatistics Statistics { get; init; }

        /// <summary>
        /// Bootstrap percentile intervals keyed by statistic (answerRate, spearman, pearson,
        /// kendallTau, qwk, mae, exactMatchRate, auc).
        /// </summary>
        [JsonPropertyName("confidenceIntervals")]
        public Dictionary<string, Interval> ConfidenceIntervals { get; init; } = new Dictionary<string, Interval>();
    }
}
