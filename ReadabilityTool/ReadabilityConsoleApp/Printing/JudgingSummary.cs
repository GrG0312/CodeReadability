using CodeReadabilityLib.Reporting.Judgements;

namespace ReadabilityConsoleApp.Printing
{
    /// <summary>
    /// Prints a plain completion summary of a judgement file: answers given and failures
    /// per run. Deliberately no ranking, because no statistics exist yet at this point.
    /// </summary>
    public static class JudgingSummary
    {
        public static void Print(JudgementReport report)
        {
            if (report.Runs.Count == 0)
            {
                Console.WriteLine("No runs were completed.");
                return;
            }

            int nameWidth = Math.Max(3, report.Runs.Max(run => run.RunName.Length));

            Console.WriteLine();
            Console.WriteLine($"{"Run".PadRight(nameWidth)}  {"Answered",8}  {"Failed",6}");
            Console.WriteLine(new string('-', nameWidth + 18));

            foreach (JudgedRun run in report.Runs)
            {
                int failed = run.ConcreteAnswers.Count(answer => answer.ModelAnswer is null);
                int answered = run.ConcreteAnswers.Count - failed;
                Console.WriteLine($"{run.RunName.PadRight(nameWidth)}  {answered,8}  {failed,6}");
            }

            Console.WriteLine();
        }
    }
}
