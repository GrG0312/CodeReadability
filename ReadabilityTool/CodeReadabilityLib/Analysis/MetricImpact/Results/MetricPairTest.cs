using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// A test of whether the scores follow metric A more strongly than metric B.
    /// </summary>
    public sealed record MetricPairTest
    {
        [JsonPropertyName("metricA")]
        public required string MetricA { get; init; }

        [JsonPropertyName("metricB")]
        public required string MetricB { get; init; }

        /// <summary>
        /// Strength of A minus strength of B (absolute correlations).
        /// </summary>
        [JsonPropertyName("strengthDifference")]
        public double? StrengthDifference { get; init; }

        /// <summary>
        /// Two-sided paired bootstrap p-value for a zero difference.
        /// </summary>
        [JsonPropertyName("pValue")]
        public double? PValue { get; init; }

        /// <summary>
        /// <see cref="PValue"/> after Holm correction across every pair of the ranking.
        /// </summary>
        [JsonPropertyName("adjustedPValue")]
        public double? AdjustedPValue { get; init; }

        [JsonPropertyName("significant")]
        public bool Significant { get; init; }

        /// <summary>
        /// The metric followed significantly more strongly, or <see langword="null"/>.
        /// </summary>
        [JsonPropertyName("stronger")]
        public string? Stronger { get; init; }
    }
}
