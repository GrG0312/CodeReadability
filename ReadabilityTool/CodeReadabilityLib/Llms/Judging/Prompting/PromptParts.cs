namespace CodeReadabilityLib.Llms.Judging.Prompting
{
    /// <summary>
    /// Prompt fragments shared by several strategies, so that they differ only where the
    /// technique requires it.
    /// </summary>
    internal static class PromptParts
    {
        public static string LanguageLine(ReadabilityEvaluationRequest request)
        {
            return request.Language is { Length: > 0 } language ? $"Language: {language}\n\n" : string.Empty;
        }

        public static string CodeBlock(string code)
        {
            return "-----BEGIN-CODE-----\n" + code + "\n-----END-CODE-----\n\n";
        }

        public static string Scope =>
            "Judge only how easy the code is to read and understand — not correctness, performance, or style preference.";

        /// <summary>
        /// The opening of every system prompt: the role, the reply format and the scale.
        /// </summary>
        public static string RaterIntroduction =>
            "You are a strict, deterministic code readability rater. " +
            $"Reply with ONLY compact JSON: {{\"score\": int}}, where score is an integer from {ReadabilityScale.Min} to {ReadabilityScale.Max}. " +
            $"{ReadabilityScale.Min} = very poor readability, {ReadabilityScale.Max} = excellent readability. " +
            Scope;

        /// <summary>
        /// The JSON reply for an example with the given score, as the model should produce it.
        /// </summary>
        public static string ScoreReply(int score)
        {
            return "{\"score\": " + score + "}\n\n";
        }
    }
}
