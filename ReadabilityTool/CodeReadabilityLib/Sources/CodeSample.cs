using CodeReadabilityLib.Languages;

namespace CodeReadabilityLib.Sources
{
    /// <summary>
    /// The single unit of work shared by both pipelines.
    /// </summary>
    public sealed record CodeSample
    {
        public required string Id { get; init; }
        public required string Code { get; init; }
        public required SupportedLanguage Language { get; init; }

        /// <summary>
        /// Human-assigned ground-truth score, when the sample comes from a labelled dataset.
        /// </summary>
        public double? ReferenceScore { get; init; }

        public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    }
}
