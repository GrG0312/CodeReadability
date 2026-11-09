namespace CodeReadabilityLib.Llms.Judging.Prompting.Strategies
{
    /// <summary>
    /// Asks for a score directly, with no guidance beyond the scale's endpoints.
    /// </summary>
    ///
    /// <remarks>
    /// The baseline every other strategy is compared against.
    /// </remarks>
    public sealed class ZeroShotStrategy : IPromptingStrategy
    {
        public string Id => "zero-shot";

        public string Description => "Direct request for a score with only the scale endpoints defined (baseline).";

        public PromptSpecification Build(ReadabilityEvaluationRequest request)
        {
            string userPrompt =
                PromptParts.LanguageLine(request) +
                "Rate the readability of this code:\n" +
                PromptParts.CodeBlock(request.Code) +
                "Return only the JSON object.";

            return new PromptSpecification { SystemPrompt = PromptParts.RaterIntroduction, UserPrompt = userPrompt };
        }
    }
}
