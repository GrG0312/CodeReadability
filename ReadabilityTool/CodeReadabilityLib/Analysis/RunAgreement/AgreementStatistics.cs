using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.RunAgreement
{
    /// <summary>
    /// The summary statistics describing how well one run agreed with the human scores.
    /// </summary>
    ///
    /// <remarks>
    /// Every statistic is nullable and is written as <see langword="null"/> whenever it could
    /// not be computed, rather than being filled in with a placeholder. This keeps
    /// "we could not measure this" distinguishable from "we measured this and got zero",
    /// which matters when the numbers are aggregated or cited.
    /// </remarks>
    public sealed record AgreementStatistics
    {
        /// <summary>
        /// The number of snippets whose answers were usable and therefore contributed to
        /// the statistics below.
        /// </summary>
        [JsonPropertyName("evaluated")]
        public int Evaluated { get; init; }

        /// <summary>
        /// The number of snippets the model gave no usable answer for: cut off before it
        /// answered, no score in the reply, or a score outside the scale. These are excluded
        /// from the agreement statistics below and counted by <see cref="AnswerRate"/>.
        /// </summary>
        [JsonPropertyName("unusable")]
        public int Unusable { get; init; }

        /// <summary>
        /// The share of snippets the model answered usably, from 0 to 1. A failure to answer
        /// within the assigned tokens is a limitation of the model, so this is a result in
        /// its own right, next to how well the given answers agree with the humans.
        /// </summary>
        [JsonPropertyName("answerRate")]
        public double? AnswerRate { get; init; }

        /// <summary>
        /// The unusable answers by kind of failure (see <c>FailureKinds</c>).
        /// </summary>
        [JsonPropertyName("failures")]
        public Dictionary<string, int> Failures { get; init; } = new Dictionary<string, int>();

        /// <summary>
        /// Spearman's rank correlation against the reference scores. This is the headline
        /// figure the models are ranked by.
        /// </summary>
        [JsonPropertyName("spearman")]
        public double? Spearman { get; init; }

        /// <summary>
        /// The two-sided p-value accompanying <see cref="Spearman"/>.
        /// </summary>
        [JsonPropertyName("pValue")]
        public double? PValue { get; init; }

        [JsonPropertyName("pearson")]
        public double? Pearson { get; init; }

        /// <summary>
        /// Kendall's tau-b against the reference scores.
        /// </summary>
        [JsonPropertyName("kendallTau")]
        public double? KendallTau { get; init; }

        [JsonPropertyName("meanAbsoluteError")]
        public double? MeanAbsoluteError { get; init; }

        /// <summary>
        /// The percentage of answers less than one point away from the reference score.
        /// </summary>
        [JsonPropertyName("exactMatchRate")]
        public double? ExactMatchRate { get; init; }

        /// <summary>
        /// The AUC for separating snippets whose reference score is above
        /// <see cref="AucThreshold"/> from the rest.
        /// </summary>
        [JsonPropertyName("auc")]
        public double? Auc { get; init; }

        /// <summary>
        /// The reference score above which a snippet counts as readable for the AUC: the
        /// median reference score of the whole dataset.
        /// </summary>
        [JsonPropertyName("aucThreshold")]
        public double? AucThreshold { get; init; }

        /// <summary>
        /// Quadratic weighted kappa against the reference scores, correcting for
        /// chance-level agreement.
        /// </summary>
        [JsonPropertyName("quadraticWeightedKappa")]
        public double? QuadraticWeightedKappa { get; init; }
    }
}
