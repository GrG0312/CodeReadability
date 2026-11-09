using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.RunAgreement
{
    /// <summary>
    /// A named agreement statistic.
    /// </summary>
    public sealed record StatisticDefinition
    {
        /// <summary>
        /// The key used in the statistics file, such as <c>spearman</c>.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// The statistic's position in <see cref="AgreementSnapshot.ToArray"/>.
        /// </summary>
        public required int Index { get; init; }

        public required bool HigherIsBetter { get; init; }

        /// <summary>
        /// Reads the statistic from a run's computed statistics.
        /// </summary>
        public required Func<AgreementStatistics, double?> Read { get; init; }
    }
}
