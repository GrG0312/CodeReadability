namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// Correlation coefficients and the rank transform they share.
    /// </summary>
    ///
    /// <remarks>
    /// Every method returns <see cref="double.NaN"/> rather than throwing when a coefficient
    /// is undefined (for example when one series is constant). Callers inside a bootstrap
    /// loop hit such degenerate resamples routinely and simply discard them.
    /// </remarks>
    public static class Correlation
    {
        /// <summary>
        /// The Pearson product-moment correlation between two equal-length series.
        /// </summary>
        public static double Pearson(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            int n = a.Count;
            if (n != b.Count || n < 2)
            {
                return double.NaN;
            }

            double meanA = 0, meanB = 0;
            for (int i = 0; i < n; i++)
            {
                meanA += a[i];
                meanB += b[i];
            }
            meanA /= n;
            meanB /= n;

            double covariance = 0;
            double varianceA = 0;
            double varianceB = 0;

            for (int i = 0; i < n; i++)
            {
                double deviationA = a[i] - meanA;
                double deviationB = b[i] - meanB;

                covariance += deviationA * deviationB;
                varianceA += deviationA * deviationA;
                varianceB += deviationB * deviationB;
            }

            double denominator = Math.Sqrt(varianceA * varianceB);
            return denominator == 0 ? double.NaN : covariance / denominator;
        }

        /// <summary>
        /// Spearman's rank correlation: the Pearson correlation of the mid-ranks.
        /// </summary>
        public static double Spearman(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            if (a.Count != b.Count || a.Count < 2)
            {
                return double.NaN;
            }

            return Pearson(Rank(a), Rank(b));
        }

        /// <summary>
        /// Kendall's tau-b, which corrects for the ties that dominate a 1-5 rating scale.
        /// </summary>
        ///
        /// <remarks>
        /// A direct O(n²) pair count. Datasets in this project hold a few hundred snippets,
        /// where this is fast enough even inside a bootstrap loop, and it is far easier to
        /// verify than Knight's O(n log n) algorithm.
        /// </remarks>
        public static double KendallTauB(IReadOnlyList<double> a, IReadOnlyList<double> b)
        {
            int n = a.Count;
            if (n != b.Count || n < 2)
            {
                return double.NaN;
            }

            long concordant = 0, discordant = 0, tiedA = 0, tiedB = 0;

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    int signA = Math.Sign(a[i] - a[j]);
                    int signB = Math.Sign(b[i] - b[j]);

                    if (signA == 0 && signB == 0)
                    {
                        continue;
                    }

                    if (signA == 0)
                    {
                        tiedA++;
                    }
                    else if (signB == 0)
                    {
                        tiedB++;
                    }
                    else if (signA == signB)
                    {
                        concordant++;
                    }
                    else
                    {
                        discordant++;
                    }
                }
            }

            double denominator = Math.Sqrt((double)(concordant + discordant + tiedA) * (concordant + discordant + tiedB));
            return denominator == 0 ? double.NaN : (concordant - discordant) / denominator;
        }

        /// <summary>
        /// The partial correlation of x and y controlling for z, from the three pairwise
        /// coefficients.
        /// </summary>
        ///
        /// <remarks>
        /// Applied to Spearman coefficients this gives the partial rank correlation, which
        /// is how the metric-impact analysis separates "the model tracks this metric" from
        /// "the humans track this metric and the model tracks the humans".
        /// </remarks>
        public static double Partial(double rxy, double rxz, double ryz)
        {
            double denominator = Math.Sqrt((1 - rxz * rxz) * (1 - ryz * ryz));
            return denominator == 0 || double.IsNaN(denominator)
                ? double.NaN
                : (rxy - rxz * ryz) / denominator;
        }

        /// <summary>
        /// Averages correlation coefficients through the Fisher z-transform, which is the
        /// conventional way to pool coefficients from several samples.
        /// </summary>
        ///
        /// <returns>
        /// The pooled coefficient, or <see cref="double.NaN"/> when no finite value was given.
        /// </returns>
        public static double FisherMean(IEnumerable<double> coefficients)
        {
            double sum = 0;
            int count = 0;

            foreach (double r in coefficients)
            {
                if (double.IsNaN(r))
                {
                    continue;
                }

                // Clamped so that a perfect correlation does not map to an infinite z.
                sum += Math.Atanh(Math.Clamp(r, -0.999999, 0.999999));
                count++;
            }

            return count == 0 ? double.NaN : Math.Tanh(sum / count);
        }

        /// <summary>
        /// Converts <paramref name="values"/> into ranks, where tied values all receive the
        /// mean of the ranks they collectively span.
        /// </summary>
        ///
        /// <returns>
        /// An array of 1-based ranks positionally aligned with <paramref name="values"/>.
        /// </returns>
        public static double[] Rank(IReadOnlyList<double> values)
        {
            int count = values.Count;

            // Indices of the values in ascending order of the value they point at.
            int[] order = new int[count];
            double[] keys = new double[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
                keys[i] = values[i];
            }
            Array.Sort(keys, order);

            double[] ranks = new double[count];

            int start = 0;
            while (start < count)
            {
                // Walk forward over every value tied with the one at 'start'.
                int end = start;
                while (end + 1 < count && keys[end + 1] == keys[start])
                {
                    end++;
                }

                double averageRank = (start + end) / 2.0 + 1.0;

                for (int i = start; i <= end; i++)
                {
                    ranks[order[i]] = averageRank;
                }

                start = end + 1;
            }

            return ranks;
        }

        /// <summary>
        /// The sizes of every group of tied values, used by the tie corrections of rank tests.
        /// </summary>
        internal static IEnumerable<int> TieGroupSizes(IEnumerable<double> values)
        {
            return values
                .GroupBy(v => v)
                .Select(g => g.Count())
                .Where(size => size > 1);
        }
    }
}
