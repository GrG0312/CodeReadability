namespace CodeReadabilityLib.Preflight.Models
{
    /// <summary>
    /// The outcome of checking one model file.
    /// </summary>
    public sealed record ModelCheckResult
    {
        /// <summary>
        /// The model file name without <c>.gguf</c>.
        /// </summary>
        public required string ModelFile { get; init; }

        public bool Loaded { get; init; }

        /// <summary>
        /// Every problem found; empty when the model is ready to use.
        /// </summary>
        public List<string> Problems { get; init; } = new List<string>();

        public List<PassCheckResult> Passes { get; init; } = new List<PassCheckResult>();

        /// <summary>
        /// The largest number of tokens one request needs (prompt, answer and thinking), and
        /// the context available to one request.
        /// </summary>
        public int? LargestRequestTokens { get; init; }

        public int? ContextPerRequest { get; init; }

        public bool Ok => Problems.Count == 0;
    }
}
