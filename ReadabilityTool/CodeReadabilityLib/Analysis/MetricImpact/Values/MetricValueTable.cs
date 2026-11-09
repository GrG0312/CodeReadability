using CodeReadabilityLib.Reporting.MetricResults;
using System.Collections.Concurrent;

namespace CodeReadabilityLib.Analysis.MetricImpact.Values
{
    /// <summary>
    /// Every metric's values, aligned with a fixed list of snippets. A missing value is NaN.
    /// </summary>
    ///
    /// <remarks>
    /// LLM-based metrics are graded by several models. Their value is an average of the
    /// graders' grades: over all graders for analyses of the human scores, and over every
    /// grader except the analysed model's own file (leave-one-out) for analyses of a model,
    /// so that a model's results never contain the model agreeing with itself.
    /// </remarks>
    internal sealed class MetricValueTable
    {
        private readonly Dictionary<string, SnippetMetrics> _snippets;
        private readonly ConcurrentDictionary<string, double[]> _cache = new ConcurrentDictionary<string, double[]>();

        /// <summary>
        /// The snippet ids the series are aligned with.
        /// </summary>
        public IReadOnlyList<string> SnippetIds { get; }

        public IReadOnlyList<MetricInfo> Metrics { get; }

        /// <summary>
        /// The model files that graded the LLM-based metrics.
        /// </summary>
        public IReadOnlyList<string> Graders { get; }

        public MetricValueTable(MetricsReport report, IReadOnlyList<string> snippetIds)
        {
            _snippets = report.Snippets.DistinctBy(s => s.Id).ToDictionary(s => s.Id, StringComparer.Ordinal);
            SnippetIds = snippetIds;
            Graders = report.Graders;

            Metrics = report.AlgorithmicMetrics
                .Select(id => new MetricInfo { MetricId = id, Kind = MetricInfo.AlgorithmicKind })
                .Concat(report.LlmMetrics.Select(id => new MetricInfo { MetricId = id, Kind = MetricInfo.LlmKind }))
                .ToList();
        }

        /// <summary>
        /// The metric's values for analysing the human scores: LLM-based metrics averaged
        /// over every grader.
        /// </summary>
        public double[] ForHumans(MetricInfo metric)
        {
            return metric.IsLlm ? LlmAverage(metric.MetricId, excludedGrader: null) : Algorithmic(metric.MetricId);
        }

        /// <summary>
        /// The metric's values for analysing one model: LLM-based metrics averaged over every
        /// grader except <paramref name="modelFile"/>.
        /// </summary>
        public double[] ForModel(MetricInfo metric, string modelFile)
        {
            return metric.IsLlm ? LlmAverage(metric.MetricId, excludedGrader: modelFile) : Algorithmic(metric.MetricId);
        }

        /// <summary>
        /// One grader's own grades of an LLM-based metric.
        /// </summary>
        public double[] OwnGrades(string metricId, string modelFile)
        {
            return _cache.GetOrAdd($"own|{metricId}|{modelFile}", _ => SnippetIds
                .Select(id => _snippets.TryGetValue(id, out SnippetMetrics? s)
                              && s.Llm.TryGetValue(modelFile, out Dictionary<string, double>? grades)
                              && grades.TryGetValue(metricId, out double value)
                    ? value
                    : double.NaN)
                .ToArray());
        }

        private double[] Algorithmic(string metricId)
        {
            return _cache.GetOrAdd($"algo|{metricId}", _ => SnippetIds
                .Select(id => _snippets.TryGetValue(id, out SnippetMetrics? s) && s.Algorithmic.TryGetValue(metricId, out double value)
                    ? value
                    : double.NaN)
                .ToArray());
        }

        private double[] LlmAverage(string metricId, string? excludedGrader)
        {
            return _cache.GetOrAdd($"llm|{metricId}|{excludedGrader}", _ => SnippetIds
                .Select(id =>
                {
                    if (!_snippets.TryGetValue(id, out SnippetMetrics? s))
                    {
                        return double.NaN;
                    }

                    double[] grades = s.Llm
                        .Where(entry => !string.Equals(entry.Key, excludedGrader, StringComparison.Ordinal))
                        .Select(entry => entry.Value.TryGetValue(metricId, out double value) ? value : double.NaN)
                        .Where(double.IsFinite)
                        .ToArray();

                    return grades.Length == 0 ? double.NaN : grades.Average();
                })
                .ToArray());
        }
    }
}
