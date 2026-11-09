namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// A percentile confidence interval.
    /// </summary>
    public readonly record struct ConfidenceInterval(double Lower, double Upper)
    {
        public bool Contains(double value) => value >= Lower && value <= Upper;
    }

    /// <summary>
    /// Nonparametric bootstrap resampling over snippets.
    /// </summary>
    ///
    /// <remarks>
    /// The bootstrap is used because the statistics compared here (rank correlations of
    /// heavily tied 1-5 ratings, kappa, AUC) have no convenient closed-form standard error,
    /// and because two models scored on the same snippets are correlated with each other,
    /// which invalidates the textbook tests for comparing two independent correlations.
    /// Resampling snippets jointly for both models preserves that dependence.
    /// </remarks>
    public static class Bootstrap
    {
        /// <summary>
        /// Draws <paramref name="iterations"/> resamples of the indices 0..n-1 and evaluates
        /// <paramref name="statistic"/> on each.
        /// </summary>
        ///
        /// <param name="statistic">
        /// Receives the resampled indices and returns one or more statistics. A NaN result
        /// (a degenerate resample) is kept as NaN and ignored by the summaries.
        /// </param>
        ///
        /// <returns>
        /// One array per statistic, each holding <paramref name="iterations"/> values.
        /// </returns>
        public static double[][] Resample(
            int n,
            int iterations,
            int statisticCount,
            Func<int[], double[]> statistic,
            int seed)
        {
            Random random = new Random(seed);
            double[][] results = new double[statisticCount][];
            for (int s = 0; s < statisticCount; s++)
            {
                results[s] = new double[iterations];
            }

            int[] indices = new int[n];

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < n; i++)
                {
                    indices[i] = random.Next(n);
                }

                double[] values = statistic(indices);
                for (int s = 0; s < statisticCount; s++)
                {
                    results[s][iteration] = values[s];
                }
            }

            return results;
        }

        /// <summary>
        /// The percentile interval covering <c>1 - alpha</c> of the finite bootstrap values.
        /// </summary>
        ///
        /// <returns>
        /// The interval, or <see langword="null"/> when fewer than half of the resamples
        /// produced a finite value and the interval would therefore be unreliable.
        /// </returns>
        public static ConfidenceInterval? PercentileInterval(IReadOnlyList<double> values, double alpha)
        {
            double[] finite = values.Where(double.IsFinite).OrderBy(v => v).ToArray();

            if (finite.Length < Math.Max(2, values.Count / 2))
            {
                return null;
            }

            return new ConfidenceInterval(
                Quantile(finite, alpha / 2),
                Quantile(finite, 1 - alpha / 2));
        }

        /// <summary>
        /// The two-sided bootstrap p-value for the null hypothesis that the statistic is zero,
        /// read off as twice the smaller tail of the bootstrap distribution beyond zero.
        /// </summary>
        ///
        /// <remarks>
        /// One is added to both counts so that the p-value can never be reported as exactly
        /// zero, which the bootstrap cannot justify with a finite number of resamples.
        /// </remarks>
        public static double TwoSidedPValue(IReadOnlyList<double> values)
        {
            double[] finite = values.Where(double.IsFinite).ToArray();
            if (finite.Length == 0)
            {
                return double.NaN;
            }

            int atOrBelow = finite.Count(v => v <= 0);
            int atOrAbove = finite.Count(v => v >= 0);

            double tail = (Math.Min(atOrBelow, atOrAbove) + 1.0) / (finite.Length + 1.0);
            return Math.Min(1.0, 2 * tail);
        }

        /// <summary>
        /// A stable seed for one named bootstrap, so that results are reproducible run to run
        /// and do not depend on the order in which parallel work happens to be scheduled.
        /// </summary>
        public static int DeriveSeed(int baseSeed, string key)
        {
            // FNV-1a, because string.GetHashCode is randomised per process.
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in key)
                {
                    hash = (hash ^ c) * 16777619;
                }

                return (int)(hash ^ (uint)baseSeed) & int.MaxValue;
            }
        }

        /// <summary>
        /// Linear-interpolation quantile of an already sorted array.
        /// </summary>
        private static double Quantile(double[] sorted, double probability)
        {
            double position = probability * (sorted.Length - 1);
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            double weight = position - lower;

            return sorted[lower] + (sorted[upper] - sorted[lower]) * weight;
        }
    }
}
