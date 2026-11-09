namespace CodeReadabilityLib.Datasets
{
    /// <summary>
    /// Represents a single code snippet.
    /// </summary>
    public sealed record CodeSnippet
    {
        /// <summary>
        /// The unique identifier for the code snippet.
        /// </summary>
        public string Id { get; init; }

        /// <summary>
        /// The actual code text of the snippet as a <see langword="string"/>
        /// </summary>
        public string Code { get; init; }

        /// <summary>
        /// The original (baseline) readability score of a simple code snippet.
        /// </summary>
        public double? Score { get; init; }

        public CodeSnippet(string id, string code, double? score = null)
        {
            Id = id;
            Code = code;
            Score = score;
        }
    }
}
