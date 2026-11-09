namespace CodeReadabilityLib.Preflight.Basics
{
    /// <summary>
    /// What a dataset holds, as far as the up-front checks need to know.
    /// </summary>
    public sealed record DatasetSummary
    {
        public required int Snippets { get; init; }

        /// <summary>
        /// Snippets with a human score.
        /// </summary>
        public required int Labelled { get; init; }

        /// <summary>
        /// The longest snippet's code, used to check that every prompt fits the context.
        /// </summary>
        public required string LongestCode { get; init; }

        /// <summary>
        /// A typical snippet's code (the median length), used for test requests.
        /// </summary>
        public required string SampleCode { get; init; }
    }
}
