namespace CodeReadabilityLib.Llms.Judging
{
    /// <summary>
    /// Defines a contract for evaluating the readability of text content.
    /// </summary>
    public interface IReadabilityJudge
    {
        /// <summary>
        /// Gets the name of the readability judge implementation.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Asynchronously evaluates the readability of the specified content and returns a list of readability scores.
        /// </summary>
        /// 
        /// <param name="request">
        /// The request containing the content and evaluation parameters to be analyzed. Cannot be null.
        /// </param>
        /// 
        /// <param name="ctoken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// 
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains a <see cref="JudgementResult"/> object with the readability scores.
        /// </returns>
        public Task<JudgementResult> EvaluateAsync(ReadabilityEvaluationRequest request, CancellationToken ctoken = default);
    }
}
