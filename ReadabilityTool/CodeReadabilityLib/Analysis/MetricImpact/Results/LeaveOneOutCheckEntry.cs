using CodeReadabilityLib.Analysis.Shared;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.MetricImpact.Results
{
    /// <summary>
    /// For one LLM-based metric, how a model's correlation changes when its own grades are
    /// kept in the metric's averaged value instead of being left out.
    /// </summary>
    ///
    /// <remarks>
    /// A check on the leave-one-out averaging: a small difference means that the model's
    /// metric profile does not come from the model agreeing with itself. Descriptive only;
    /// no test is applied, the interval shows the size of the difference.
    /// </remarks>
    public sealed record LeaveOneOutCheckEntry
    {
        [JsonPropertyName("metricId")]
        public required string MetricId { get; init; }

        /// <summary>
        /// The correlation with the metric averaged over every grader except the model's own
        /// file, as used everywhere else for this model.
        /// </summary>
        [JsonPropertyName("spearmanLeaveOneOut")]
        public double? SpearmanLeaveOneOut { get; init; }

        /// <summary>
        /// The correlation with the metric averaged over every grader, the model's own file
        /// included.
        /// </summary>
        [JsonPropertyName("spearmanAllGraders")]
        public double? SpearmanAllGraders { get; init; }

        /// <summary>
        /// <see cref="SpearmanAllGraders"/> minus <see cref="SpearmanLeaveOneOut"/>: what the
        /// model's own grades add to the correlation.
        /// </summary>
        [JsonPropertyName("difference")]
        public double? Difference { get; init; }

        /// <summary>
        /// The paired bootstrap confidence interval of <see cref="Difference"/>.
        /// </summary>
        [JsonPropertyName("differenceInterval")]
        public Interval? DifferenceInterval { get; init; }
    }
}
