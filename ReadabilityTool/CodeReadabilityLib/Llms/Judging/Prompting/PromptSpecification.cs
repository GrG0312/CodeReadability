namespace CodeReadabilityLib.Llms.Judging.Prompting
{
    /// <summary>
    /// The messages a prompting strategy sends to the model.
    /// </summary>
    public sealed record PromptSpecification
    {
        public required string SystemPrompt { get; init; }

        public required string UserPrompt { get; init; }

        /// <summary>
        /// The completion token budget for the answer itself, excluding any thinking.
        /// </summary>
        public uint MaxTokens { get; init; } = 64;
    }
}
