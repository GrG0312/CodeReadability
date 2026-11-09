using CodeReadabilityLib.Analysis;
using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Analysis.Output;
using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Reporting;
using CodeReadabilityLib.Storage.Layout;
using CodeReadabilityLib.Storage.Tracking;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Printing;
using System.Text.Json.Nodes;

namespace ReadabilityConsoleApp.Execution.Steps
{
    /// <summary>
    /// The statistics step, shared by <c>compute-statistics</c> and <c>run-pipeline</c>.
    /// </summary>
    public static class ComputeStatisticsStep
    {
        /// <returns>
        /// The written statistics file.
        /// </returns>
        public static async Task<string> RunAsync(
            CommandLine args,
            ILogger logger,
            StatisticsInputs inputs,
            RunFolder run,
            CancellationToken cancellationToken)
        {
            AnalysisOptions options = SharedOptions.Analysis(args);

            Provenance provenance = new Provenance
            {
                DatasetId = inputs.Judgements.Provenance.DatasetId,
                DatasetName = inputs.Judgements.Provenance.DatasetName,
                Inputs = new Dictionary<string, string>
                {
                    ["judgements"] = inputs.JudgementPath,
                    ["metrics"] = inputs.MetricsPath
                },
                RunFolder = run.Path
            };

            StatisticsSection section = new StatisticsComputer(options, logger).Compute(inputs, provenance);

            // A copy of the judgement file exactly as read, with the statistics added.
            JsonObject copy = inputs.JudgementJson;
            copy["statistics"] = JsonReportWriter.ToNode(section);

            string outputPath = run.OutputFile(
                OutputLayout.StatisticsFolder, Path.GetFileNameWithoutExtension(inputs.JudgementPath), "stats");
            await JsonReportWriter.WriteAsync(copy, outputPath, cancellationToken).ConfigureAwait(false);

            StatisticsSummary.Print(section);
            logger.Info($"Statistics written to {outputPath}");

            return outputPath;
        }
    }
}
