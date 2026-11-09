using System.Text;

namespace CodeReadabilityLib.Llms.Judging.Prompting.Strategies
{
    /// <summary>
    /// A zero-shot request with very detailed instructions: the criteria to consider and a
    /// written description of every score level.
    /// </summary>
    ///
    /// <remarks>
    /// Replaces the model's private notion of "readable" with a shared, stated standard,
    /// without showing any example code.
    /// </remarks>
    public sealed class ZeroShotDetailedStrategy : IPromptingStrategy
    {
        /// <summary>
        /// One description per score, from <see cref="ReadabilityScale.Min"/> to
        /// <see cref="ReadabilityScale.Max"/>.
        /// </summary>
        private static readonly string[] LevelDescriptions =
        [
            "Very poor: hard to follow even with effort; cryptic or misleading names, tangled or deeply nested control flow, inconsistent layout, no helpful comments.",
            "Poor: understandable only with significant effort; several unclear names, long or convoluted logic, distracting formatting.",
            "Acceptable: understandable with moderate effort; mostly reasonable names and structure, but with noticeable problems.",
            "Good: easy to follow; clear names, sensible structure and consistent formatting, with only minor issues.",
            "Excellent: immediately understandable; descriptive names, simple and well-organised logic, clean formatting, comments where they help."
        ];

        public string Id => "zero-shot-detailed";

        public string Description => "Zero-shot with detailed instructions: explicit criteria and a description of every score level.";

        public PromptSpecification Build(ReadabilityEvaluationRequest request)
        {
            StringBuilder levels = new StringBuilder();
            for (int score = ReadabilityScale.Min; score <= ReadabilityScale.Max; score++)
            {
                levels.Append("- ").Append(score).Append(" = ").AppendLine(LevelDescriptions[score - ReadabilityScale.Min]);
            }

            string systemPrompt =
                PromptParts.RaterIntroduction + "\n\n" +
                "Consider these criteria:\n" +
                "- Naming: do identifiers describe their purpose?\n" +
                "- Structure and control flow: is the logic simple, short and shallowly nested?\n" +
                "- Formatting: are indentation, spacing and line length consistent and easy on the eye?\n" +
                "- Comments and documentation: do they explain what is not obvious from the code?\n" +
                "- Idioms: does the code use the language's familiar constructs?\n\n" +
                "Score levels:\n" +
                levels;

            string userPrompt =
                PromptParts.LanguageLine(request) +
                "Rate the readability of this code using the criteria and score levels:\n" +
                PromptParts.CodeBlock(request.Code) +
                "Return only the JSON object.";

            return new PromptSpecification { SystemPrompt = systemPrompt, UserPrompt = userPrompt };
        }
    }
}
