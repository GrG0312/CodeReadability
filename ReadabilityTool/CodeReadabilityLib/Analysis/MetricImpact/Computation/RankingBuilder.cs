using CodeReadabilityLib.Analysis.MetricImpact.Results;
using CodeReadabilityLib.Analysis.MetricImpact.Values;
using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.MetricImpact.Computation
{
    /// <summary>
    /// Turns correlation sets into rankings, pairwise metric tests and model-versus-human
    /// comparisons.
    /// </summary>
    internal sealed class RankingBuilder
    {
        private readonly IReadOnlyList<MetricInfo> _metrics;
        private readonly IReadOnlySet<string> _lowAgreement;
        private readonly AnalysisOptions _options;

        /// <param name="lowAgreement">
        /// LLM-based metrics whose graders agree poorly, to be flagged in every ranking.
        /// </param>
        public RankingBuilder(IReadOnlyList<MetricInfo> metrics, IReadOnlySet<string> lowAgreement, AnalysisOptions options)
        {
            _metrics = metrics;
            _lowAgreement = lowAgreement;
            _options = options;
        }

        /// <summary>
        /// Ranks the metrics by the absolute correlation, strongest first.
        /// </summary>
        ///
        /// <param name="human">
        /// The human correlations to compare priorities with, or <see langword="null"/> for
        /// the human ranking itself.
        /// </param>
        public MetricRanking Build(CorrelationSet set, CorrelationSet? human)
        {
            int[]?[] bootstrapRanks = BootstrapRanks(set);
            int?[] ranks = Ranks(set.Point);

            List<RankedMetric> ranked = _metrics
                .Select((metric, m) => new RankedMetric
                {
                    MetricId = metric.MetricId,
                    Kind = metric.Kind,
                    Spearman = AnalysisFormat.Finite(set.Point[m]),
                    Strength = AnalysisFormat.Finite(Math.Abs(set.Point[m])),
                    Rank = ranks[m],
                    RankInterval = RankInterval(bootstrapRanks, m),
                    LowGraderAgreement = _lowAgreement.Contains(metric.MetricId)
                })
                .OrderBy(metric => metric.Rank ?? int.MaxValue)
                .ToList();

            return new MetricRanking
            {
                Metrics = ranked,
                PairTests = PairTests(set),
                SamePrioritiesAsHumans = human is null ? null : SamePriorities(set.Point, human.Point)
            };
        }

        /// <summary>
        /// Per metric, the signed difference between the model's and the human correlation,
        /// Holm-corrected across the metrics.
        /// </summary>
        public List<ModelVersusHuman> VersusHuman(CorrelationSet model, CorrelationSet human)
        {
            List<ModelVersusHuman> raw = new List<ModelVersusHuman>();

            for (int m = 0; m < _metrics.Count; m++)
            {
                double difference = model.Point[m] - human.Point[m];
                double[] distribution = model.Bootstrap[m].Zip(human.Bootstrap[m], (a, b) => a - b).ToArray();
                ConfidenceInterval? interval = double.IsFinite(difference)
                    ? Statistics.Bootstrap.PercentileInterval(distribution, _options.Alpha)
                    : null;

                raw.Add(new ModelVersusHuman
                {
                    MetricId = _metrics[m].MetricId,
                    ModelSpearman = AnalysisFormat.Finite(model.Point[m]),
                    HumanSpearman = AnalysisFormat.Finite(human.Point[m]),
                    Difference = AnalysisFormat.Finite(difference),
                    DifferenceInterval = interval is ConfidenceInterval ci ? new Interval { Lower = ci.Lower, Upper = ci.Upper } : null,
                    PValue = double.IsFinite(difference) ? AnalysisFormat.Finite(Statistics.Bootstrap.TwoSidedPValue(distribution)) : null
                });
            }

            double[] adjusted = HypothesisTests.HolmAdjust(raw.Select(r => r.PValue ?? double.NaN).ToArray());

            return raw
                .Select((r, i) => r with
                {
                    AdjustedPValue = AnalysisFormat.Finite(adjusted[i]),
                    Significant = adjusted[i] < _options.Alpha && r.Difference is double d && d != 0
                })
                .ToList();
        }

        /// <summary>
        /// The Spearman correlation between two rankings' metric strengths.
        /// </summary>
        public static double? SamePriorities(double[] point, double[] human)
        {
            List<double> a = new List<double>();
            List<double> b = new List<double>();
            for (int m = 0; m < point.Length; m++)
            {
                if (double.IsFinite(point[m]) && double.IsFinite(human[m]))
                {
                    a.Add(Math.Abs(point[m]));
                    b.Add(Math.Abs(human[m]));
                }
            }

            return a.Count < 3 ? null : AnalysisFormat.Finite(Correlation.Spearman(a, b));
        }

        /// <summary>
        /// 1-based ranks by descending strength; metrics without a correlation get none.
        /// </summary>
        private static int?[] Ranks(IReadOnlyList<double> correlations)
        {
            int?[] ranks = new int?[correlations.Count];
            int rank = 1;
            foreach (int m in Enumerable.Range(0, correlations.Count)
                .Where(m => double.IsFinite(correlations[m]))
                .OrderByDescending(m => Math.Abs(correlations[m])))
            {
                ranks[m] = rank++;
            }

            return ranks;
        }

        /// <summary>
        /// Per resample, every metric's rank.
        /// </summary>
        private int[]?[] BootstrapRanks(CorrelationSet set)
        {
            int iterations = set.Bootstrap.Length == 0 ? 0 : set.Bootstrap[0].Length;
            int[]?[] ranks = new int[]?[_metrics.Count];

            for (int m = 0; m < _metrics.Count; m++)
            {
                ranks[m] = double.IsFinite(set.Point[m]) ? new int[iterations] : null;
            }

            double[] resample = new double[_metrics.Count];
            for (int b = 0; b < iterations; b++)
            {
                for (int m = 0; m < _metrics.Count; m++)
                {
                    resample[m] = set.Bootstrap[m][b];
                }

                int?[] resampleRanks = Ranks(resample);
                for (int m = 0; m < _metrics.Count; m++)
                {
                    if (ranks[m] is int[] metricRanks)
                    {
                        // A metric with no correlation in this resample ranks last.
                        metricRanks[b] = resampleRanks[m] ?? _metrics.Count;
                    }
                }
            }

            return ranks;
        }

        private Interval? RankInterval(int[]?[] bootstrapRanks, int m)
        {
            if (bootstrapRanks[m] is not int[] ranks || ranks.Length == 0)
            {
                return null;
            }

            return Statistics.Bootstrap.PercentileInterval(ranks.Select(rank => (double)rank).ToArray(), _options.Alpha) is ConfidenceInterval ci
                ? new Interval { Lower = ci.Lower, Upper = ci.Upper }
                : null;
        }

        /// <summary>
        /// Every pair of metrics: does one have a significantly larger absolute correlation?
        /// Holm-corrected across the pairs.
        /// </summary>
        private List<MetricPairTest> PairTests(CorrelationSet set)
        {
            List<MetricPairTest> raw = new List<MetricPairTest>();

            for (int a = 0; a < _metrics.Count; a++)
            {
                for (int b = a + 1; b < _metrics.Count; b++)
                {
                    if (!double.IsFinite(set.Point[a]) || !double.IsFinite(set.Point[b]))
                    {
                        continue;
                    }

                    double difference = Math.Abs(set.Point[a]) - Math.Abs(set.Point[b]);
                    double[] distribution = set.Bootstrap[a]
                        .Zip(set.Bootstrap[b], (x, y) => Math.Abs(x) - Math.Abs(y))
                        .ToArray();

                    raw.Add(new MetricPairTest
                    {
                        MetricA = _metrics[a].MetricId,
                        MetricB = _metrics[b].MetricId,
                        StrengthDifference = AnalysisFormat.Finite(difference),
                        PValue = AnalysisFormat.Finite(Statistics.Bootstrap.TwoSidedPValue(distribution))
                    });
                }
            }

            double[] adjusted = HypothesisTests.HolmAdjust(raw.Select(r => r.PValue ?? double.NaN).ToArray());

            return raw
                .Select((r, i) =>
                {
                    bool significant = adjusted[i] < _options.Alpha && r.StrengthDifference is double d && d != 0;
                    return r with
                    {
                        AdjustedPValue = AnalysisFormat.Finite(adjusted[i]),
                        Significant = significant,
                        Stronger = significant ? (r.StrengthDifference > 0 ? r.MetricA : r.MetricB) : null
                    };
                })
                .ToList();
        }
    }
}
