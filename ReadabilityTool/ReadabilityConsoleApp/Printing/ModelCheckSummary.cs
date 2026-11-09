using CodeReadabilityLib.Preflight.Models;
using System.Globalization;

namespace ReadabilityConsoleApp.Printing
{
    /// <summary>
    /// Prints the per-model report of <c>check-models</c>, with an estimate of how long
    /// judging would take.
    /// </summary>
    public static class ModelCheckSummary
    {
        /// <param name="snippets">
        /// Snippets to estimate the judging time for.
        /// </param>
        ///
        /// <param name="strategies">
        /// Prompting strategies per pass.
        /// </param>
        public static void Print(IReadOnlyList<ModelCheckResult> results, int snippets, int strategies, int concurrency, bool snippetsFromDataset)
        {
            Console.WriteLine();
            Console.WriteLine("MODEL CHECK");

            double totalSeconds = 0;
            bool estimateComplete = true;

            foreach (ModelCheckResult result in results)
            {
                Console.WriteLine($"  {result.ModelFile}: {(result.Ok ? "OK" : "PROBLEMS")}");

                // A model that did not load has no passes, and so no time to add.
                if (result.Passes.Count == 0)
                {
                    estimateComplete = false;
                }

                if (result.LargestRequestTokens is int largest && result.ContextPerRequest is int perRequest)
                {
                    Console.WriteLine($"    largest request ≈ {largest} tokens, {perRequest} available per request");
                }

                double modelSeconds = 0;
                foreach (PassCheckResult pass in result.Passes)
                {
                    string speed = pass.SecondsPerAnswer is double seconds ? $"{seconds.ToString("0.0", CultureInfo.InvariantCulture)} s per answer" : "no answer";
                    string thinking = pass.ThinkingSwitchWorks switch
                    {
                        true => ", thinking switch works",
                        false => ", thinking switch DOES NOT work",
                        null => string.Empty
                    };
                    Console.WriteLine($"    {pass.ModelName}: {pass.AnsweredStrategies.Count}/{strategies} strategies answered, {speed}{thinking}");

                    // Not problems: a failed answer is a model limitation the statistics count.
                    foreach ((string strategy, string kind) in pass.FailedStrategies)
                    {
                        Console.WriteLine($"      no answer with {strategy} ({kind})");
                    }

                    if (pass.SecondsPerAnswer is double perAnswer)
                    {
                        modelSeconds += perAnswer * snippets * strategies / Math.Max(1, concurrency);
                    }
                    else
                    {
                        estimateComplete = false;
                    }
                }

                if (modelSeconds > 0)
                {
                    // What this model's own job takes in multi-job mode.
                    Console.WriteLine($"    estimated mass-judge time for this model: about {Format(TimeSpan.FromSeconds(modelSeconds))}");
                }

                totalSeconds += modelSeconds;

                foreach (string problem in result.Problems)
                {
                    Console.WriteLine($"    problem: {problem}");
                }
            }

            Console.WriteLine();
            string basis = snippetsFromDataset ? $"{snippets} labelled snippets" : $"{snippets} snippets (no --dataset given)";
            if (totalSeconds == 0 && !estimateComplete)
            {
                Console.WriteLine($"No estimate of the mass-judge time for {basis}: no model could be tested.");
                Console.WriteLine();
                return;
            }

            Console.WriteLine(
                $"Estimated mass-judge time for {basis}, {strategies} strategies, concurrency {concurrency}, all models one after " +
                $"another (one-job mode): about {Format(TimeSpan.FromSeconds(totalSeconds))}{(estimateComplete ? string.Empty : " (excluding models that could not be tested)")}.");
            Console.WriteLine("In multi-job mode the models run in parallel, so the longest single model decides.");
            Console.WriteLine("The estimate assumes answers take as long as the test answers; thinking runs vary a lot.");
            Console.WriteLine();
        }

        private static string Format(TimeSpan time)
        {
            return time.TotalHours >= 1 ? $"{(int)time.TotalHours} h {time.Minutes} min" : $"{Math.Max(1, (int)Math.Ceiling(time.TotalMinutes))} min";
        }
    }
}
