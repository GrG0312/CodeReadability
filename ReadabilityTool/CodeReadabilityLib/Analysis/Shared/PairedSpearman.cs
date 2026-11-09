using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.Shared
{
    /// <summary>
    /// Spearman correlation of two snippet-aligned series in which a missing value is NaN.
    /// </summary>
    internal static class PairedSpearman
    {
        /// <summary>
        /// The minimum number of complete pairs for a correlation to be computed.
        /// </summary>
        public const int MinimumPairs = 4;

        /// <summary>
        /// Spearman over the snippets where both series have a value.
        /// </summary>
        ///
        /// <param name="indices">
        /// A bootstrap resample of snippet indices, or <see langword="null"/> for every
        /// snippet once.
        /// </param>
        ///
        /// <returns>
        /// The correlation, or NaN with fewer than <see cref="MinimumPairs"/> complete pairs
        /// or when either series is constant.
        /// </returns>
        public static double Compute(double[] x, double[] y, int[]? indices = null)
        {
            int length = indices?.Length ?? x.Length;
            List<double> a = new List<double>(length);
            List<double> b = new List<double>(length);

            for (int i = 0; i < length; i++)
            {
                int index = indices?[i] ?? i;
                double xi = x[index];
                double yi = y[index];
                if (double.IsFinite(xi) && double.IsFinite(yi))
                {
                    a.Add(xi);
                    b.Add(yi);
                }
            }

            return a.Count < MinimumPairs ? double.NaN : Correlation.Spearman(a, b);
        }
    }
}
