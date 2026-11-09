using CodeReadabilityLib.Languages;

namespace CodeReadabilityLib.Metrics
{
    /// <summary>
    /// Represents a metric definition that is calculated using an algorithm.
    /// </summary>
    public record AlgorithmicMetricDefinition : MetricDefinition
    {
        public required Func<string, SupportedLanguage, double> Algorithm { get; init; }
    }
}
