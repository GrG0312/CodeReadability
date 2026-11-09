using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.MetricImpact.Computation
{
    /// <summary>
    /// The Spearman correlation of every metric with one set of scores, plus its bootstrap
    /// distribution.
    /// </summary>
    internal sealed class CorrelationSet
    {
        /// <summary>
        /// One correlation per metric, in the order of the metric list.
        /// </summary>
        public double[] Point { get; }

        /// <summary>
        /// Per metric, one correlation per bootstrap resample.
        /// </summary>
        public double[][] Bootstrap { get; }

        private CorrelationSet(double[] point, double[][] bootstrap)
        {
            Point = point;
            Bootstrap = bootstrap;
        }

        /// <summary>
        /// Correlates every metric series with <paramref name="scores"/> on the full sample
        /// and on every resample of <paramref name="bootstrap"/>.
        /// </summary>
        public static CorrelationSet Compute(IReadOnlyList<double[]> metricValues, double[] scores, SnippetBootstrap bootstrap)
        {
            int metrics = metricValues.Count;
            double[] point = new double[metrics];
            double[][] distribution = new double[metrics][];

            for (int m = 0; m < metrics; m++)
            {
                point[m] = PairedSpearman.Compute(metricValues[m], scores);
                distribution[m] = new double[bootstrap.Iterations];
            }

            Parallel.For(0, bootstrap.Iterations, b =>
            {
                int[] indices = bootstrap.Resamples[b];
                for (int m = 0; m < metrics; m++)
                {
                    distribution[m][b] = double.IsFinite(point[m])
                        ? PairedSpearman.Compute(metricValues[m], scores, indices)
                        : double.NaN;
                }
            });

            return new CorrelationSet(point, distribution);
        }

        /// <summary>
        /// Pools several sets through the Fisher z-transform, metric by metric and resample by
        /// resample.
        /// </summary>
        public static CorrelationSet FisherMean(IReadOnlyList<CorrelationSet> sets)
        {
            int metrics = sets[0].Point.Length;
            int iterations = sets[0].Bootstrap.Length == 0 ? 0 : sets[0].Bootstrap[0].Length;

            double[] point = new double[metrics];
            double[][] distribution = new double[metrics][];

            for (int m = 0; m < metrics; m++)
            {
                point[m] = Correlation.FisherMean(sets.Select(set => set.Point[m]));
                distribution[m] = new double[iterations];
                for (int b = 0; b < iterations; b++)
                {
                    distribution[m][b] = Correlation.FisherMean(sets.Select(set => set.Bootstrap[m][b]));
                }
            }

            return new CorrelationSet(point, distribution);
        }
    }
}
