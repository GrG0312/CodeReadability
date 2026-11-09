namespace CodeReadabilityLib.Llms.Judging.Prompting
{
    /// <summary>
    /// One way of asking a model for a readability score.
    /// </summary>
    ///
    /// <remarks>
    /// Every strategy asks the same question on the same fixed scale and differs only in how
    /// it is asked, so that differences in agreement can be attributed to the prompt.
    /// </remarks>
    public interface IPromptingStrategy
    {
        /// <summary>
        /// A short, stable identifier recorded in output files, such as <c>zero-shot</c>.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// One sentence describing the technique, for output files and help text.
        /// </summary>
        string Description { get; }

        PromptSpecification Build(ReadabilityEvaluationRequest request);
    }
}
