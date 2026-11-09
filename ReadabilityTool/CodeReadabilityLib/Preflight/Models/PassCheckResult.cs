namespace CodeReadabilityLib.Preflight.Models
{
    /// <summary>
    /// The check of one judging pass of a model: the whole model, or one mode of a hybrid.
    /// </summary>
    public sealed record PassCheckResult
    {
        public required string ModelName { get; init; }

        /// <summary>
        /// The strategies whose test request got a valid answer.
        /// </summary>
        public List<string> AnsweredStrategies { get; init; } = new List<string>();

        /// <summary>
        /// The strategies whose test answer failed, with the kind of failure. Not a problem:
        /// failing to answer is a model limitation the statistics count.
        /// </summary>
        public Dictionary<string, string> FailedStrategies { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// Average seconds per test answer, or <see langword="null"/> when none succeeded.
        /// </summary>
        public double? SecondsPerAnswer { get; init; }

        /// <summary>
        /// For a hybrid pass, whether the reply showed thinking as expected; otherwise
        /// <see langword="null"/>.
        /// </summary>
        public bool? ThinkingSwitchWorks { get; init; }
    }
}
