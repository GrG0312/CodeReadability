using CodeReadabilityLib.Analysis.MetricImpact.Results;
using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Reporting.MetricResults;
using CodeReadabilityLib.Statistics.Reliability;

namespace CodeReadabilityLib.Analysis.MetricImpact.Computation
{
    /// <summary>
    /// Measures how far the grader models agree on each LLM-based metric.
    /// </summary>
    internal static class GraderAgreementCalculator
    {
        /// <summary>
        /// The ICC below which agreement counts as poor and the metric is flagged.
        /// </summary>
        public const double PoorThreshold = 0.5;

        /// <summary>
        /// One entry per LLM-based metric, over every snippet of the metrics file graded by
        /// every grader.
        /// </summary>
        public static List<GraderAgreement> Compute(MetricsReport report)
        {
            List<GraderAgreement> agreements = new List<GraderAgreement>();

            foreach (string metricId in report.LlmMetrics)
            {
                List<double[]> rows = new List<double[]>();
                foreach (SnippetMetrics snippet in report.Snippets)
                {
                    double[] row = report.Graders
                        .Select(grader => snippet.Llm.TryGetValue(grader, out Dictionary<string, double>? grades)
                                          && grades.TryGetValue(metricId, out double value)
                            ? value
                            : double.NaN)
                        .ToArray();

                    if (row.All(double.IsFinite))
                    {
                        rows.Add(row);
                    }
                }

                double icc = IntraclassCorrelation.ConsistencyOfAverage(rows);

                agreements.Add(new GraderAgreement
                {
                    MetricId = metricId,
                    Icc = AnalysisFormat.Finite(icc),
                    Band = IntraclassCorrelation.Band(icc),
                    Graders = report.Graders.Count,
                    Snippets = rows.Count,
                    Flagged = double.IsFinite(icc) && icc < PoorThreshold
                });
            }

            return agreements;
        }
    }
}
