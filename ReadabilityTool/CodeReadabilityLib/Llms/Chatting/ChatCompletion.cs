namespace CodeReadabilityLib.Llms.Chatting
{
    /// <summary>
    /// A model's reply with the details beyond its text.
    /// </summary>
    public sealed record ChatCompletion
    {
        /// <summary>
        /// The reply text.
        /// </summary>
        public required string Content { get; init; }

        /// <summary>
        /// The model's thinking, when the server returns it separately from the reply.
        /// </summary>
        public string? ReasoningContent { get; init; }

        /// <summary>
        /// Why generation stopped, such as <c>stop</c> or <c>length</c> (ran out of tokens).
        /// </summary>
        public string? FinishReason { get; init; }

        /// <summary>
        /// Whether the reply shows any thinking, either returned separately or left in the
        /// text as a <c>&lt;think&gt;</c> block.
        /// </summary>
        public bool ShowsThinking =>
            !string.IsNullOrWhiteSpace(ReasoningContent) ||
            Content.Contains("<think>", StringComparison.OrdinalIgnoreCase);
    }
}
