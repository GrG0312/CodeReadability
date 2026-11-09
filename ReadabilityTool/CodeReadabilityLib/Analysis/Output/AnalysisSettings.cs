using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Output
{
    /// <summary>
    /// The settings the statistics were computed with, recorded for reproducibility.
    /// </summary>
    public sealed record AnalysisSettings
    {
        [JsonPropertyName("bootstrapIterations")]
        public required int BootstrapIterations { get; init; }

        /// <summary>
        /// The family-wise significance level. Confidence intervals cover 1 - alpha.
        /// </summary>
        [JsonPropertyName("alpha")]
        public required double Alpha { get; init; }

        [JsonPropertyName("seed")]
        public required int Seed { get; init; }

        /// <summary>
        /// The human score above which a snippet counts as readable for the AUC.
        /// </summary>
        [JsonPropertyName("readableThreshold")]
        public double? ReadableThreshold { get; init; }
    }
}
