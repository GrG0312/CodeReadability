using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Shared
{
    /// <summary>
    /// A test of whether configuration B agrees with the humans better than configuration A
    /// on the same snippets.
    /// </summary>
    ///
    /// <remarks>
    /// When several runs are pooled (several prompting strategies, or several models), each
    /// configuration's value is the mean Spearman over its runs, and the bootstrap resamples
    /// snippets jointly for all of them.
    /// </remarks>
    public sealed record PairwiseComparison
    {
        /// <summary>
        /// What is held fixed, such as "qwen3.5 9B flash, pooled over 4 strategies".
        /// </summary>
        [JsonPropertyName("context")]
        public required string Context { get; init; }

        [JsonPropertyName("labelA")]
        public required string LabelA { get; init; }

        [JsonPropertyName("labelB")]
        public required string LabelB { get; init; }

        [JsonPropertyName("runsA")]
        public List<string> RunsA { get; init; } = new List<string>();

        [JsonPropertyName("runsB")]
        public List<string> RunsB { get; init; } = new List<string>();

        /// <summary>
        /// Snippets for which every compared run produced a usable answer.
        /// </summary>
        [JsonPropertyName("pairedSnippets")]
        public int PairedSnippets { get; init; }

        [JsonPropertyName("spearmanA")]
        public double? SpearmanA { get; init; }

        [JsonPropertyName("spearmanB")]
        public double? SpearmanB { get; init; }

        /// <summary>
        /// Spearman of B minus Spearman of A; positive means B agrees better.
        /// </summary>
        [JsonPropertyName("spearmanDifference")]
        public double? SpearmanDifference { get; init; }

        [JsonPropertyName("differenceInterval")]
        public Interval? DifferenceInterval { get; init; }

        /// <summary>
        /// Two-sided paired bootstrap p-value for a zero difference.
        /// </summary>
        [JsonPropertyName("pValue")]
        public double? PValue { get; init; }

        /// <summary>
        /// <see cref="PValue"/> after Holm correction across the comparison family.
        /// </summary>
        [JsonPropertyName("adjustedPValue")]
        public double? AdjustedPValue { get; init; }

        [JsonPropertyName("maeA")]
        public double? MaeA { get; init; }

        [JsonPropertyName("maeB")]
        public double? MaeB { get; init; }

        /// <summary>
        /// Wilcoxon signed-rank p-value on the per-snippet absolute errors.
        /// </summary>
        [JsonPropertyName("maePValue")]
        public double? MaePValue { get; init; }

        [JsonPropertyName("maeAdjustedPValue")]
        public double? MaeAdjustedPValue { get; init; }

        /// <summary>
        /// Whether the rank-agreement difference survives the Holm correction.
        /// </summary>
        [JsonPropertyName("significant")]
        public bool Significant { get; init; }

        /// <summary>
        /// The label of the configuration that agrees better, when the difference is
        /// significant; otherwise <see langword="null"/>.
        /// </summary>
        [JsonPropertyName("better")]
        public string? Better { get; init; }

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; init; } = string.Empty;
    }
}
