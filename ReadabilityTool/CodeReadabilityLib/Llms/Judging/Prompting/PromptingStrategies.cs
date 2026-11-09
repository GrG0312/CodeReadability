using CodeReadabilityLib.Llms.Judging.Prompting.Strategies;

namespace CodeReadabilityLib.Llms.Judging.Prompting
{
    /// <summary>
    /// The registry of available prompting strategies.
    /// </summary>
    public static class PromptingStrategies
    {
        /// <summary>
        /// Every strategy, baseline first, then in order of how much guidance they give.
        /// </summary>
        public static readonly IReadOnlyList<IPromptingStrategy> All =
        [
            new ZeroShotStrategy(),
            new ZeroShotDetailedStrategy(),
            new OneShotStrategy(),
            new FewShotStrategy()
        ];

        public static string SupportedIds => string.Join(", ", All.Select(s => s.Id));

        /// <summary>
        /// Resolves a comma-separated list of strategy ids, or every strategy when
        /// <paramref name="ids"/> is empty or <c>all</c>.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when an id does not name a strategy.
        /// </exception>
        public static IReadOnlyList<IPromptingStrategy> Parse(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids) || ids.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return All;
            }

            List<IPromptingStrategy> selected = new List<IPromptingStrategy>();

            foreach (string id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                IPromptingStrategy strategy = All.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"Unknown prompting strategy '{id}'. Supported: {SupportedIds}.");

                if (!selected.Contains(strategy))
                {
                    selected.Add(strategy);
                }
            }

            return selected;
        }
    }
}
