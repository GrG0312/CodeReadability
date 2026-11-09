using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Metrics
{
    /// <summary>
    /// Represents a score for a specific metric, including the raw value and an optional normalized value.
    /// </summary>
    public sealed record MetricScore
    {
        [JsonPropertyName("metricId")]
        public required string MetricId { get; init; }

        [JsonPropertyName("value")]
        public required double Value { get; init; }

        /// <summary>
        /// The value rescaled onto 0-100 with higher always meaning better, or
        /// <see langword="null"/> when the metric has no natural bounds to rescale against.
        /// </summary>
        ///
        /// <remarks>
        /// Present only for metrics that are bounded by definition, such as percentages.
        /// A counting metric like lines of code is reported through <see cref="Value"/>
        /// alone, because 7 lines means seven lines and there is no defensible ceiling that
        /// would turn it into a score. Analyses should prefer <see cref="Value"/>;
        /// <see cref="Normalized"/> exists so that the bounded metrics can be compared on a
        /// single axis with a consistent direction.
        /// </remarks>
        [JsonPropertyName("normalized")]
        public double? Normalized { get; init; }


        /// <summary>
        /// Creates a raw metric score.
        /// </summary>
        /// 
        /// <param name="id">
        /// The unique identifier of the metric.
        /// </param>
        /// 
        /// <param name="value">
        /// The raw value of the metric score without any normalization.
        /// </param>
        /// 
        /// <returns>
        /// A new instance of <see cref="MetricScore"/> with the specified metric ID and value.
        /// </returns>
        public static MetricScore Raw(string id, double value)
        {
            return new MetricScore() { MetricId = id, Value = value };
        }

        /// <summary>
        /// Creates a metric score from a metric definition and a raw value, normalizing the value based on the definition's min and max values.
        /// </summary>
        /// 
        /// <param name="definition">
        /// The metric definition that provides the min and max values for normalization.
        /// </param>
        /// 
        /// <param name="value">
        /// The raw value of the metric score to be normalized.
        /// </param>
        /// 
        /// <returns>
        /// A new instance of <see cref="MetricScore"/> with the specified metric ID, raw value, and normalized value (if applicable).
        /// </returns>
        public static MetricScore FromDefinition(MetricDefinition definition, double value)
        {
            double? normalized = null;

            // Convert the definition's min and max values to local variables
            // and check if they are valid for normalization
            if (definition is { MinValue: int min, MaxValue: int max } && max > min)
            {
                double clamped = Math.Clamp(value, min, max);
                double scaled = (clamped - min) / (max - min) * 100.0;

                if (definition.HigherIsBetter)
                {
                    normalized = scaled;
                }
                else
                {
                    normalized = 100.0 - scaled;
                }
            }

            return new MetricScore { MetricId = definition.Id, Value = value, Normalized = normalized };
        }
    }
}
