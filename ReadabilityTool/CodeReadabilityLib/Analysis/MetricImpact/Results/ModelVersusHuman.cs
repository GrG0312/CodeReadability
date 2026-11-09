using CodeReadabilityLib.Analysis.Shared;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// Whether a model follows one metric significantly more or less than the humans do.
    /// </summary>
    ///
    /// <remarks>
    /// Signed, so that a model and the humans going in opposite directions is detected. For
    /// an LLM-based metric both correlations use the same leave-one-out values.
    /// </remarks>
    public sealed record ModelVersusHuman
    {
        [JsonPropertyName("metricId")]
        public required string MetricId { get; init; }

        [JsonPropertyName("modelSpearman")]
        public double? ModelSpearman { get; init; }

        [JsonPropertyName("humanSpearman")]
        public double? HumanSpearman { get; init; }

        /// <summary>
        /// Model minus human, signed.
        /// </summary>
        [JsonPropertyName("difference")]
        public double? Difference { get; init; }

        [JsonPropertyName("differenceInterval")]
        public Interval? DifferenceInterval { get; init; }

        [JsonPropertyName("pValue")]
        public double? PValue { get; init; }

        /// <summary>
        /// <see cref="PValue"/> after Holm correction across the metrics.
        /// </summary>
        [JsonPropertyName("adjustedPValue")]
        public double? AdjustedPValue { get; init; }

        [JsonPropertyName("significant")]
        public bool Significant { get; init; }
    }
}
