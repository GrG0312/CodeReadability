namespace CodeReadabilityLib.Reporting.MetricResults
{
    /// <summary>
    /// Which kinds of metrics a metrics file contains: the <c>--mode</c> it was made with.
    /// </summary>
    public static class MetricKinds
    {
        /// <summary>
        /// Only the algorithmic metrics, computed from the code itself.
        /// </summary>
        public const string AlgorithmicOnly = "algo-only";

        /// <summary>
        /// Only the LLM-based metrics, graded by the models.
        /// </summary>
        public const string LlmOnly = "llm-only";

        /// <summary>
        /// Both kinds. The default.
        /// </summary>
        public const string Both = "both";

        /// <summary>
        /// Resolves a <c>--mode</c> value, defaulting to <see cref="Both"/>.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the value is not one of the three kinds.
        /// </exception>
        public static string Parse(string? mode)
        {
            string normalized = (mode ?? Both).Trim().ToLowerInvariant();
            return normalized is AlgorithmicOnly or LlmOnly or Both
                ? normalized
                : throw new ArgumentException($"--mode must be {AlgorithmicOnly}, {LlmOnly} or {Both}, got '{mode}'.");
        }

        public static bool IncludesAlgorithmic(string kinds)
        {
            return kinds is AlgorithmicOnly or Both;
        }

        public static bool IncludesLlm(string kinds)
        {
            return kinds is LlmOnly or Both;
        }
    }
}
