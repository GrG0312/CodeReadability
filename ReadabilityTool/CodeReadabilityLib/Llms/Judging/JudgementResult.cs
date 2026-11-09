using CodeReadabilityLib.Metrics;

namespace CodeReadabilityLib.Llms.Judging
{
    /// <summary>
    /// Represents the result of a judgement operation, indicating whether it succeeded or failed, along with associated scores and optional raw response or failure reason.
    /// </summary>
    public sealed record JudgementResult
    {
        /// <summary>
        /// Indicates whether the judgement operation succeeded. 
        /// If true, the Scores property will contain the evaluation results; 
        /// if false, the FailureReason property will provide details about the failure.
        /// </summary>
        public required bool Succeeded { get; init; }

        /// <summary>
        /// Gets the list of metric scores resulting from the judgement operation.
        /// </summary>
        public IReadOnlyList<MetricScore> Scores { get; init; } = Array.Empty<MetricScore>();

        /// <summary>
        /// Gets the raw response from the judgement operation, if available. This can be useful for debugging or logging purposes.
        /// </summary>
        public string? RawResponse { get; init; }

        /// <summary>
        /// Gets the reason for failure if the judgement operation did not succeed. 
        /// This property is null when <see cref="Succeeded"/> is true.
        /// </summary>
        public string? FailureReason { get; init; }

        /// <summary>
        /// Why the judgement failed, one of <see cref="FailureKinds"/>; <see langword="null"/>
        /// when it succeeded.
        /// </summary>
        public string? FailureKind { get; init; }

        /// <summary>
        /// Creates a successful <see cref="JudgementResult"/> with the provided scores and optional raw response.
        /// </summary>
        /// 
        /// <param name="scores">
        /// The list of metric scores resulting from the successful judgement operation.
        /// </param>
        /// 
        /// <param name="rawResponse">
        /// An optional raw response from the judgement operation, useful for debugging or logging purposes.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="JudgementResult"/> instance representing a successful judgement operation.
        /// </returns>
        public static JudgementResult Ok(IReadOnlyList<MetricScore> scores, string? rawResponse = null)
        {
            return new JudgementResult
            {
                Succeeded = true,
                Scores = scores,
                RawResponse = rawResponse
            };
        }

        /// <summary>
        /// Creates a failed <see cref="JudgementResult"/> with the provided failure reason and optional raw response.
        /// </summary>
        ///
        /// <param name="failureKind">
        /// Why the judgement failed, one of <see cref="FailureKinds"/>.
        /// </param>
        ///
        /// <param name="failureReason">
        /// The reason for the failure of the judgement operation. This should provide insight into why the operation did not succeed.
        /// </param>
        /// 
        /// <param name="rawResponse">
        /// An optional raw response from the judgement operation, useful for debugging or logging purposes.
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="JudgementResult"/> instance representing a failed judgement operation.
        /// </returns>
        public static JudgementResult Failed(string failureKind, string failureReason, string? rawResponse = null)
        {
            return new JudgementResult
            {
                Succeeded = false,
                FailureKind = failureKind,
                FailureReason = failureReason,
                RawResponse = rawResponse
            };
        }
    }
}
