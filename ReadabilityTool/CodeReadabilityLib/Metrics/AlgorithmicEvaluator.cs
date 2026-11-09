using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Logging;

namespace CodeReadabilityLib.Metrics
{
    /// <summary>
    /// Represents an evaluator for algorithmic complexity and performance metrics.
    /// </summary>
    public sealed class AlgorithmicEvaluator
    {
        private readonly ILogger _logger;

        public AlgorithmicEvaluator(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Evaluates the provided <paramref name="code"/> using algorithmic metrics for the specified programming <paramref name="language"/>.
        /// </summary>
        /// 
        /// <returns>
        /// A read-only list of <see cref="MetricScore"/> instances representing the results of the evaluation.
        /// </returns>
        public IReadOnlyList<MetricScore> Evaluate(string code, SupportedLanguage language)
        {
            List<MetricScore> scores = new List<MetricScore>();

            foreach (AlgorithmicMetricDefinition metric in MetricRegistry.GetAlgorithmicForLanguage(language.Language))
            {
                try
                {
                    scores.Add(MetricScore.FromDefinition(metric, metric.Algorithm(code, language)));
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Algorithmic metric '{metric.Id}' failed: {ex.Message}");
                }
            }

            return scores;
        }
    }
}
