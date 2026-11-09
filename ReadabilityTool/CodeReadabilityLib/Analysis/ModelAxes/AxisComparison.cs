using CodeReadabilityLib.Analysis.Shared;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.ModelAxes
{
    /// <summary>
    /// All comparisons along one axis of model variation.
    /// </summary>
    public sealed record AxisComparison
    {
        /// <summary>
        /// One of <c>quantization</c>, <c>size</c>, <c>reasoning</c> or <c>family</c>.
        /// </summary>
        [JsonPropertyName("axis")]
        public required string Axis { get; init; }

        [JsonPropertyName("description")]
        public required string Description { get; init; }

        /// <summary>
        /// One comparison per model pair, pooled over every prompting strategy both models
        /// were run with. This is the primary answer for the axis.
        /// </summary>
        [JsonPropertyName("comparisons")]
        public List<PairwiseComparison> Comparisons { get; init; } = new List<PairwiseComparison>();

        /// <summary>
        /// The same model pairs compared separately under each strategy. Empty when only
        /// one strategy was run, where it would duplicate <see cref="Comparisons"/>.
        /// </summary>
        [JsonPropertyName("perStrategyComparisons")]
        public List<PairwiseComparison> PerStrategyComparisons { get; init; } = new List<PairwiseComparison>();

        /// <summary>
        /// Models with no counterpart differing only along this axis.
        /// </summary>
        [JsonPropertyName("unmatchedModels")]
        public List<string> UnmatchedModels { get; init; } = new List<string>();

        [JsonPropertyName("summary")]
        public string Summary { get; init; } = string.Empty;
    }
}
