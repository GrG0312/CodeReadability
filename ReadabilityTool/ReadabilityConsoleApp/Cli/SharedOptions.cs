using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Llms.Judging.Prompting;
using CodeReadabilityLib.Reporting.MetricResults;
using System.Globalization;

namespace ReadabilityConsoleApp.Cli
{
    /// <summary>
    /// Reads and validates the options several commands share, so that an invalid value is
    /// reported before any work starts.
    /// </summary>
    public static class SharedOptions
    {
        public const int DefaultThinkingBudget = 2048;

        public static uint ThinkingBudget(CommandLine args)
        {
            return (uint)Math.Max(0, args.Int("thinking-budget", DefaultThinkingBudget));
        }

        /// <exception cref="ArgumentException">
        /// Thrown when a strategy id is unknown.
        /// </exception>
        public static IReadOnlyList<IPromptingStrategy> Strategies(CommandLine args)
        {
            return PromptingStrategies.Parse(args.Optional("strategies"));
        }

        /// <exception cref="ArgumentException">
        /// Thrown when the mode is not algo-only, llm-only or both.
        /// </exception>
        public static string MetricKindsOf(CommandLine args)
        {
            return MetricKinds.Parse(args.Optional("mode"));
        }

        /// <exception cref="ArgumentException">
        /// Thrown when alpha is not between 0 and 1.
        /// </exception>
        public static AnalysisOptions Analysis(CommandLine args)
        {
            return new AnalysisOptions
            {
                BootstrapIterations = Math.Max(100, args.Int("bootstrap", 2000)),
                Alpha = Alpha(args.Optional("alpha")),
                Seed = args.Int("seed", 12345)
            };
        }

        private static double Alpha(string? raw)
        {
            if (raw is null)
            {
                return 0.05;
            }

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double alpha) || alpha <= 0 || alpha >= 1)
            {
                throw new ArgumentException($"--alpha must be a number between 0 and 1, got '{raw}'.");
            }

            return alpha;
        }
    }
}
