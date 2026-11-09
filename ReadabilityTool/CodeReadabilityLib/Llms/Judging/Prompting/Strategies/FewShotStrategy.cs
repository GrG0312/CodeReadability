using CodeReadabilityLib.Llms.Judging.Prompting.Examples;
using System.Text;

namespace CodeReadabilityLib.Llms.Judging.Prompting.Strategies
{
    /// <summary>
    /// Shows the model three scored examples (poor, average, excellent) before the code to
    /// be rated.
    /// </summary>
    ///
    /// <remarks>
    /// Calibrates the model to the scale through demonstrations rather than a verbal
    /// definition.
    /// </remarks>
    public sealed class FewShotStrategy : IPromptingStrategy
    {
        public string Id => "few-shot";

        public string Description => "Three scored example snippets (poor, average, excellent) precede the code.";

        public PromptSpecification Build(ReadabilityEvaluationRequest request)
        {
            string systemPrompt = PromptParts.RaterIntroduction + " Use the scored examples to calibrate your ratings.";

            IReadOnlyList<ScoredExample> examples = ExampleSnippets.For(request.Language);

            StringBuilder user = new StringBuilder();
            user.Append(PromptParts.LanguageLine(request));

            for (int i = 0; i < examples.Count; i++)
            {
                user.Append("Example ").Append(i + 1).Append(":\n")
                    .Append(PromptParts.CodeBlock(examples[i].Code.TrimEnd()))
                    .Append(PromptParts.ScoreReply(examples[i].Score));
            }

            user.Append("Now rate the readability of this code:\n")
                .Append(PromptParts.CodeBlock(request.Code))
                .Append("Return only the JSON object.");

            return new PromptSpecification { SystemPrompt = systemPrompt, UserPrompt = user.ToString() };
        }
    }
}
