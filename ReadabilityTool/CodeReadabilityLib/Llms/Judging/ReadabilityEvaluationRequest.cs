using CodeReadabilityLib.Metrics;

namespace CodeReadabilityLib.Llms.Judging
{
    /// <summary>
    /// Represents a request to evaluate the readability of code content.
    /// </summary>
    public sealed record ReadabilityEvaluationRequest
    {
        public required string Code { get; init; }
        public string? Language { get; init; }
        public string? Context { get; init; }
        public IReadOnlyList<MetricDefinition> Metrics { get; init; } = [];
    }
}
