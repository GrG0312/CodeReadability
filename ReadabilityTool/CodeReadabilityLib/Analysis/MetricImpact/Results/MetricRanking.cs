using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// The metrics ranked by how strongly one set of scores follows them.
    /// </summary>
    public sealed record MetricRanking
    {
        /// <summary>
        /// Every metric, strongest first; metrics without a computable correlation last.
        /// </summary>
        [JsonPropertyName("metrics")]
        public List<RankedMetric> Metrics { get; init; } = new List<RankedMetric>();

        /// <summary>
        /// Every pair of metrics tested against each other, Holm-corrected within this
        /// ranking.
        /// </summary>
        [JsonPropertyName("pairTests")]
        public List<MetricPairTest> PairTests { get; init; } = new List<MetricPairTest>();

        /// <summary>
        /// The Spearman correlation between this ranking's metric strengths and the human
        /// ranking's: close to 1 means the same metrics matter in the same order as for the
        /// human scores. Absent for the human ranking itself.
        /// </summary>
        [JsonPropertyName("samePrioritiesAsHumans")]
        public double? SamePrioritiesAsHumans { get; init; }
    }
}
