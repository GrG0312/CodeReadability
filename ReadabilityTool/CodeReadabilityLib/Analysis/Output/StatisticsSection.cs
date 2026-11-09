using CodeReadabilityLib.Analysis.MetricImpact.Results;
using CodeReadabilityLib.Analysis.ModelAxes;
using CodeReadabilityLib.Analysis.Prompting;
using CodeReadabilityLib.Analysis.RunAgreement;
using CodeReadabilityLib.Storage.Tracking;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Output
{
    /// <summary>
    /// Everything <c>compute-statistics</c> adds to its copy of the judgement file, under the
    /// <c>statistics</c> key.
    /// </summary>
    public sealed record StatisticsSection
    {
        [JsonPropertyName("generatedAtUtc")]
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// The dataset ID and the judgement and metrics files the statistics were made from.
        /// </summary>
        [JsonPropertyName("provenance")]
        public required Provenance Provenance { get; init; }

        [JsonPropertyName("settings")]
        public required AnalysisSettings Settings { get; init; }

        /// <summary>
        /// Every run's agreement with the human scores, best first.
        /// </summary>
        [JsonPropertyName("runs")]
        public List<RunSummary> Runs { get; init; } = new List<RunSummary>();

        /// <summary>
        /// Comparisons along the quantization, size, reasoning and family axes.
        /// </summary>
        [JsonPropertyName("modelAxes")]
        public List<AxisComparison> ModelAxes { get; init; } = new List<AxisComparison>();

        /// <summary>
        /// Absent when only one prompting strategy was run.
        /// </summary>
        [JsonPropertyName("promptingStrategies")]
        public PromptingStrategyReport? PromptingStrategies { get; init; }

        [JsonPropertyName("metricImpact")]
        public required MetricImpactReport MetricImpact { get; init; }

        /// <summary>
        /// Analyses that were skipped or limited, and why.
        /// </summary>
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; init; } = new List<string>();
    }
}
