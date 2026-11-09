namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// The outcome of a Wilcoxon signed-rank test.
    /// </summary>
    ///
    /// <param name="N">
    /// The number of non-zero differences the test was computed on.
    /// </param>
    public readonly record struct WilcoxonResult(double WPlus, double Z, double PValue, int N);

    /// <summary>
    /// The outcome of a Friedman test.
    /// </summary>
    ///
    /// <param name="MeanRanks">
    /// The mean rank of every treatment, where rank 1 is the best, aligned with the columns
    /// of the input matrix.
    /// </param>
    public readonly record struct FriedmanResult(double ChiSquared, int DegreesOfFreedom, double PValue, int Blocks, double[] MeanRanks);

    /// <summary>
    /// Non-parametric significance tests and multiple-comparison corrections.
    /// </summary>
    public static class HypothesisTests
    {
        /// <summary>
        /// The Wilcoxon signed-rank test that paired differences are centred on zero.
        /// </summary>
        ///
        /// <remarks>
        /// Uses the normal approximation with a tie correction and discards zero differences
        /// (Wilcoxon's original treatment), matching SciPy's
        /// <c>wilcoxon(d, zero_method="wilcox", correction=False, method="approx")</c>.
        /// The approximation is accurate for the hundreds of snippets compared here; it
        /// would not be for a handful of observations.
        /// </remarks>
        ///
        /// <returns>
        /// The test result, whose p-value is <see cref="double.NaN"/> when every difference
        /// is zero.
        /// </returns>
        public static WilcoxonResult WilcoxonSignedRank(IReadOnlyList<double> differences)
        {
            double[] nonZero = differences.Where(d => d != 0 && !double.IsNaN(d)).ToArray();
            int n = nonZero.Length;

            if (n == 0)
            {
                return new WilcoxonResult(0, double.NaN, double.NaN, 0);
            }

            double[] absolute = nonZero.Select(Math.Abs).ToArray();
            double[] ranks = Correlation.Rank(absolute);

            double wPlus = 0;
            for (int i = 0; i < n; i++)
            {
                if (nonZero[i] > 0)
                {
                    wPlus += ranks[i];
                }
            }

            double mean = n * (n + 1) / 4.0;
            double tieCorrection = Correlation.TieGroupSizes(absolute).Sum(t => (double)t * t * t - t) / 48.0;
            double variance = n * (n + 1) * (2.0 * n + 1) / 24.0 - tieCorrection;

            if (variance <= 0)
            {
                return new WilcoxonResult(wPlus, double.NaN, double.NaN, n);
            }

            double z = (wPlus - mean) / Math.Sqrt(variance);
            return new WilcoxonResult(wPlus, z, NormalTwoSidedPValue(z), n);
        }

        /// <summary>
        /// The Friedman test that several treatments measured on the same blocks perform
        /// equally well.
        /// </summary>
        ///
        /// <param name="values">
        /// One row per block and one column per treatment. Higher values are better; within
        /// each block the best treatment receives rank 1.
        /// </param>
        ///
        /// <returns>
        /// The chi-squared statistic with a tie correction and its p-value. The p-value is
        /// <see cref="double.NaN"/> when there are fewer than two blocks or two treatments,
        /// though the mean ranks are still filled in for a single block.
        /// </returns>
        public static FriedmanResult Friedman(IReadOnlyList<double[]> values)
        {
            int blocks = values.Count;
            int treatments = blocks == 0 ? 0 : values[0].Length;
            double[] rankSums = new double[treatments];

            if (blocks == 0 || treatments < 2)
            {
                return new FriedmanResult(double.NaN, Math.Max(0, treatments - 1), double.NaN, blocks, rankSums);
            }

            double tieSum = 0;

            foreach (double[] row in values)
            {
                // Negated so that the largest value receives rank 1.
                double[] ranks = Correlation.Rank(row.Select(v => -v).ToArray());
                for (int j = 0; j < treatments; j++)
                {
                    rankSums[j] += ranks[j];
                }

                tieSum += Correlation.TieGroupSizes(row).Sum(t => (double)t * t * t - t);
            }

            double k = treatments;
            double b = blocks;
            double chiSquared = 12.0 / (b * k * (k + 1)) * rankSums.Sum(r => r * r) - 3 * b * (k + 1);

            double correction = 1 - tieSum / (b * (k * k * k - k));
            chiSquared = correction > 0 ? chiSquared / correction : double.NaN;

            // A single block ranks the treatments but gives no basis for a test.
            if (blocks < 2)
            {
                chiSquared = double.NaN;
            }

            int degreesOfFreedom = treatments - 1;
            double pValue = double.IsNaN(chiSquared)
                ? double.NaN
                : SpecialFunctions.UpperRegularizedGamma(degreesOfFreedom / 2.0, chiSquared / 2.0);

            double[] meanRanks = rankSums.Select(r => r / b).ToArray();
            return new FriedmanResult(chiSquared, degreesOfFreedom, pValue, blocks, meanRanks);
        }

        /// <summary>
        /// Holm-Bonferroni adjusted p-values, which control the family-wise error rate when
        /// many comparisons are tested at once.
        /// </summary>
        ///
        /// <remarks>
        /// The adjustment is what licenses a statement such as "X is certainly better than
        /// Y": without it, testing dozens of model pairs at 5% would be expected to produce
        /// false positives by chance alone. Holm is uniformly more powerful than plain
        /// Bonferroni and makes no assumption about how the tests depend on each other.
        /// </remarks>
        ///
        /// <returns>
        /// The adjusted p-values aligned with <paramref name="pValues"/>. A
        /// <see cref="double.NaN"/> input stays NaN and does not count towards the family size.
        /// </returns>
        public static double[] HolmAdjust(IReadOnlyList<double> pValues)
        {
            double[] adjusted = Enumerable.Repeat(double.NaN, pValues.Count).ToArray();

            int[] order = Enumerable.Range(0, pValues.Count)
                .Where(i => !double.IsNaN(pValues[i]))
                .OrderBy(i => pValues[i])
                .ToArray();

            int m = order.Length;
            double runningMax = 0;

            for (int rank = 0; rank < m; rank++)
            {
                int index = order[rank];
                double value = Math.Min(1.0, (m - rank) * pValues[index]);

                // Enforces monotonicity, so that a smaller raw p never gets a larger adjusted one.
                runningMax = Math.Max(runningMax, value);
                adjusted[index] = runningMax;
            }

            return adjusted;
        }

        /// <summary>
        /// The two-sided p-value of a standard normal statistic.
        /// </summary>
        public static double NormalTwoSidedPValue(double z)
        {
            return SpecialFunctions.Erfc(Math.Abs(z) / Math.Sqrt(2));
        }
    }
}
