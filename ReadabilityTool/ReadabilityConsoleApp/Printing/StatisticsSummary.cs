using CodeReadabilityLib.Analysis.MetricImpact.Results;
using CodeReadabilityLib.Analysis.ModelAxes;
using CodeReadabilityLib.Analysis.Output;
using CodeReadabilityLib.Analysis.RunAgreement;
using CodeReadabilityLib.Analysis.Shared;
using System.Globalization;

namespace ReadabilityConsoleApp.Printing
{
    /// <summary>
    /// Prints the headline results of a statistics section: the run leaderboard, the axis
    /// and strategy conclusions, and the top of the human and overall metric rankings.
    /// The full detail is in the statistics file.
    /// </summary>
    public static class StatisticsSummary
    {
        private const int TopMetrics = 5;

        public static void Print(StatisticsSection section)
        {
            PrintRuns(section.Runs);

            Console.WriteLine("MODEL AXES");
            foreach (AxisComparison axis in section.ModelAxes)
            {
                Console.WriteLine($"  {axis.Axis}: {axis.Summary}");
            }
            Console.WriteLine();

            if (section.PromptingStrategies is { } strategies)
            {
                Console.WriteLine("PROMPTING STRATEGIES");
                Console.WriteLine($"  {strategies.Conclusion}");
                foreach (PairwiseComparison comparison in strategies.OverallPairwise.Where(c => c.Significant))
                {
                    Console.WriteLine($"  {comparison.Conclusion}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("METRIC IMPACT (strongest first; sign = direction)");
            PrintRanking("  Human scores:", section.MetricImpact.Human);
            PrintRanking("  Models overall:", section.MetricImpact.Overall);

            foreach (GraderAgreement agreement in section.MetricImpact.GraderAgreement.Where(a => a.Flagged))
            {
                Console.WriteLine($"  Low grader agreement: {agreement.MetricId} (ICC {Format(agreement.Icc)})");
            }
            Console.WriteLine();

            foreach (string warning in section.Warnings)
            {
                Console.WriteLine($"Warning: {warning}");
            }
        }

        private static void PrintRuns(IReadOnlyList<RunSummary> runs)
        {
            if (runs.Count == 0)
            {
                return;
            }

            int nameWidth = Math.Max(3, runs.Max(run => run.ModelName.Length + run.PromptingStrategy.Length + 3));

            Console.WriteLine();
            Console.WriteLine($"{"Run".PadRight(nameWidth)}  {"Spearman",8}  {"QWK",6}  {"MAE",6}  {"Used",5}  {"Answered",8}");
            Console.WriteLine(new string('-', nameWidth + 44));

            foreach (RunSummary run in runs)
            {
                string name = $"{run.ModelName} [{run.PromptingStrategy}]";
                AgreementStatistics s = run.Statistics;
                string answered = s.AnswerRate is double rate ? rate.ToString("0.0%", CultureInfo.InvariantCulture) : "-";
                Console.WriteLine(
                    $"{name.PadRight(nameWidth)}  {Format(s.Spearman),8}  {Format(s.QuadraticWeightedKappa),6}  " +
                    $"{Format(s.MeanAbsoluteError),6}  {s.Evaluated,5}  {answered,8}");
            }

            Console.WriteLine("Spearman, QWK and MAE are over the answered snippets only; Answered is the share of all snippets.");

            Console.WriteLine();
        }

        private static void PrintRanking(string title, MetricRanking ranking)
        {
            Console.WriteLine(ranking.SamePrioritiesAsHumans is double same
                ? $"{title} (same priorities as humans: {Format(same)})"
                : title);

            foreach (RankedMetric metric in ranking.Metrics.Where(m => m.Rank.HasValue).Take(TopMetrics))
            {
                string flag = metric.LowGraderAgreement ? "  [low grader agreement]" : string.Empty;
                Console.WriteLine($"    {metric.Rank,2}. {metric.MetricId} (ρ = {Format(metric.Spearman, "+0.000;-0.000")}){flag}");
            }
        }

        private static string Format(double? value, string format = "0.000")
        {
            return value is double v ? v.ToString(format, CultureInfo.InvariantCulture) : "n/a";
        }
    }
}
