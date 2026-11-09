namespace CodeReadabilityLib.Analysis.Shared
{
    /// <summary>
    /// Settings shared by every analysis in <c>compute-statistics</c>.
    /// </summary>
    public sealed record AnalysisOptions
    {
        /// <summary>
        /// Bootstrap resamples per test and per confidence interval.
        /// </summary>
        public int BootstrapIterations { get; init; } = 2000;

        /// <summary>
        /// The family-wise significance level; confidence intervals cover 1 - alpha.
        /// </summary>
        public double Alpha { get; init; } = 0.05;

        /// <summary>
        /// Base seed for every bootstrap, so that the analysis is exactly reproducible.
        /// </summary>
        public int Seed { get; init; } = 12345;
    }
}
