using CodeReadabilityLib.Analysis.Shared;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// One metric's place in a ranking: how strongly the scores follow it.
    /// </summary>
    public sealed record RankedMetric
    {
        [JsonPropertyName("metricId")]
        public required string MetricId { get; init; }

        /// <summary>
        /// <c>algorithmic</c> or <c>llm</c>.
        /// </summary>
        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        /// <summary>
        /// The signed Spearman correlation between the metric and the scores. Its sign is the
        /// direction: negative means a higher metric value goes with a lower score.
        /// </summary>
        [JsonPropertyName("spearman")]
        public double? Spearman { get; init; }

        /// <summary>
        /// The absolute value of <see cref="Spearman"/>, which the ranking is ordered by.
        /// </summary>
        [JsonPropertyName("strength")]
        public double? Strength { get; init; }

        /// <summary>
        /// 1 for the metric the scores follow most strongly. Absent when the correlation
        /// could not be computed.
        /// </summary>
        [JsonPropertyName("rank")]
        public int? Rank { get; init; }

        /// <summary>
        /// Bootstrap percentile interval of the rank.
        /// </summary>
        [JsonPropertyName("rankInterval")]
        public Interval? RankInterval { get; init; }

        /// <summary>
        /// Set for an LLM-based metric whose graders agree poorly (ICC below 0.5): its values
        /// are mostly noise, so a low rank means "not measured reliably" rather than
        /// "unimportant".
        /// </summary>
        [JsonPropertyName("lowGraderAgreement")]
        public bool LowGraderAgreement { get; init; }
    }
}
