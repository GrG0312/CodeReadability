using CodeReadabilityLib.Languages;

namespace CodeReadabilityLib.Metrics
{
    /// <summary>
    /// Represents the definition of a metric used to evaluate code readability.
    /// </summary>
    public record MetricDefinition
    {
        /// <summary>
        /// A unique identifier for the metric.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// The name of the metric.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// A description of the metric, explaining what it measures and how it is used.
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        /// A list of programming languages to which this metric is applicable.
        /// If the list is empty, the metric is considered applicable to all languages.
        /// </summary>
        public IReadOnlyList<ProgLang> ApplicableLanguages { get; init; } = [];

        /// <summary>
        /// The minimum value that the metric can take. If <see langword="null"/>, there is no minimum value.
        /// </summary>
        public int? MinValue { get; init; }

        /// <summary>
        /// The maximum value that the metric can take. If <see langword="null"/>, there is no maximum value.
        /// </summary>
        ///
        /// <remarks>
        /// Set this only when the metric is bounded <i>by its own definition</i>, as a
        /// percentage is. Do not invent a ceiling for a metric that counts something:
        /// lines of code, nesting depth and cyclomatic complexity have no natural maximum,
        /// and choosing one would both saturate
        /// <see cref="MetricScore.Normalized"/> past that point and bake an arbitrary
        /// judgement into the data. Such metrics are reported by their actual value alone.
        /// </remarks>
        public int? MaxValue { get; init; }

        /// <summary>
        /// Indicates whether a higher value of the metric is considered better.
        /// If <see langword="true"/>, higher values are better; if <see langword="false"/>, lower values are better.
        /// </summary>
        public bool HigherIsBetter { get; init; } = true;


        /// <summary>
        /// Determines whether the metric is applicable to a given programming <paramref name="language"/>.
        /// </summary>
        /// 
        /// <returns>
        /// <see langword="true"/> if the metric is applicable to the specified <paramref name="language"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool AppliesTo(ProgLang language)
        {
            return ApplicableLanguages.Count == 0 || ApplicableLanguages.Contains(language);
        }
    }
}
