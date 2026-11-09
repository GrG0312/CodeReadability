namespace CodeReadabilityLib.Llms.Judging.Prompting.Examples
{
    /// <summary>
    /// A hand-written code snippet with the readability score it demonstrates.
    /// </summary>
    public sealed record ScoredExample
    {
        public required string Code { get; init; }

        /// <summary>
        /// The score shown to the model for this example, on the fixed scale.
        /// </summary>
        public required int Score { get; init; }
    }
}
