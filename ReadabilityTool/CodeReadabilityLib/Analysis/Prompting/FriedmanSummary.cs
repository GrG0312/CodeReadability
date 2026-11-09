using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Prompting
{
    /// <summary>
    /// The Friedman test of whether the prompting strategies differ at all across models.
    /// </summary>
    public sealed record FriedmanSummary
    {
        [JsonPropertyName("chiSquared")]
        public double? ChiSquared { get; init; }

        [JsonPropertyName("degreesOfFreedom")]
        public int DegreesOfFreedom { get; init; }

        [JsonPropertyName("pValue")]
        public double? PValue { get; init; }

        /// <summary>
        /// Models run with every strategy, the only ones that take part.
        /// </summary>
        [JsonPropertyName("models")]
        public int Models { get; init; }

        [JsonPropertyName("significant")]
        public bool Significant { get; init; }
    }
}
