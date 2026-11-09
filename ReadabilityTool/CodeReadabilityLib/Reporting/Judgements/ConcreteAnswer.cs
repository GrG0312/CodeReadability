using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Reporting.Judgements
{
    /// <summary>
    /// What one model answered for one snippet.
    /// </summary>
    public sealed record ConcreteAnswer
    {
        [JsonPropertyName("snippetId")]
        public required string SnippetId { get; init; }

        /// <summary>
        /// The human reference score for this snippet.
        /// </summary>
        [JsonPropertyName("originalScore")]
        public required double OriginalScore { get; init; }

        /// <summary>
        /// The model's score, or <see langword="null"/> when its response was unusable.
        /// </summary>
        ///
        /// <remarks>
        /// Never substitute a value here. The previous implementation clamped an
        /// unparsable answer to the minimum of the scale, which recorded a failure to
        /// answer as a genuine one-star rating and biased every downstream statistic.
        /// </remarks>
        [JsonPropertyName("modelAnswer")]
        public int? ModelAnswer { get; init; }

        /// <summary>
        /// What kind of failure the response was, one of <c>FailureKinds</c>, when
        /// <see cref="ModelAnswer"/> is <see langword="null"/>. Missing in files written
        /// before the kinds existed.
        /// </summary>
        [JsonPropertyName("failureKind")]
        public string? FailureKind { get; init; }

        /// <summary>
        /// Why the response was rejected, when <see cref="ModelAnswer"/> is
        /// <see langword="null"/>.
        /// </summary>
        [JsonPropertyName("failureReason")]
        public string? FailureReason { get; init; }
    }
}
