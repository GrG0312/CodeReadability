using CodeReadabilityLib.Llms.Judging.Prompting.Examples;
using System.Text;

namespace CodeReadabilityLib.Llms.Judging.Prompting.Strategies
{
    /// <summary>
    /// Shows the model one scored example, an excellent snippet, before the code to be rated.
    /// </summary>
    ///
    /// <remarks>
    /// Sits between zero-shot and few-shot: a single demonstration of the top of the scale.
    /// </remarks>
    public sealed class OneShotStrategy : IPromptingStrategy
    {
        public string Id => "one-shot";

        public string Description => "One scored example snippet (excellent) precedes the code.";

        public PromptSpecification Build(ReadabilityEvaluationRequest request)
        {
            string systemPrompt = PromptParts.RaterIntroduction + " Use the scored example to calibrate your rating.";

            ScoredExample example = ExampleSnippets.ExcellentFor(request.Language);

            StringBuilder user = new StringBuilder();
            user.Append(PromptParts.LanguageLine(request))
                .Append("Example:\n")
                .Append(PromptParts.CodeBlock(example.Code.TrimEnd()))
                .Append(PromptParts.ScoreReply(example.Score))
                .Append("Now rate the readability of this code:\n")
                .Append(PromptParts.CodeBlock(request.Code))
                .Append("Return only the JSON object.");

            return new PromptSpecification { SystemPrompt = systemPrompt, UserPrompt = user.ToString() };
        }
    }
}
