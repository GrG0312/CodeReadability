using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Datasets;
using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Pipelines.MassJudge;
using CodeReadabilityLib.Reporting;
using CodeReadabilityLib.Reporting.Judgements;
using CodeReadabilityLib.Sources;
using CodeReadabilityLib.Storage.DatasetIdentity;
using CodeReadabilityLib.Storage.Layout;
using CodeReadabilityLib.Storage.Tracking;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Execution;
using ReadabilityConsoleApp.Printing;

namespace ReadabilityConsoleApp.Execution.Steps
{
    /// <summary>
    /// The judging step, shared by <c>mass-judge</c> and <c>run-pipeline</c>.
    /// </summary>
    public static class MassJudgeStep
    {
        /// <returns>
        /// The written judgement file, or <see langword="null"/> in a per-model job.
        /// </returns>
        public static async Task<string?> RunAsync(
            CommandLine args,
            ILogger logger,
            ResolvedDataset dataset,
            RunFolder run,
            SupportedLanguage language,
            string modelDirectory,
            IReadOnlyDictionary<string, ModelDescription> descriptions,
            string? descriptionPath,
            CheckpointStore checkpoints,
            CancellationToken cancellationToken)
        {
            MassJudgeOptions options = new MassJudgeOptions
            {
                ModelDirectory = modelDirectory,
                Descriptions = descriptions,
                Strategies = SharedOptions.Strategies(args),
                Concurrency = args.Int("concurrency", 1),
                Checkpoints = checkpoints,
                OnlyModel = args.Optional("only-model"),
                ThinkingTokenBudget = SharedOptions.ThinkingBudget(args),
                ServerTemplate = ServerOptionsReader.Read(args)
            };

            Dictionary<string, string> inputs = new Dictionary<string, string> { ["modelDirectory"] = modelDirectory };
            if (descriptionPath is not null)
            {
                inputs["modelDescriptions"] = descriptionPath;
            }

            DatasetSampleSource source = new DatasetSampleSource(dataset.Path, language, DatasetLoaderFactory.Create(dataset.Path));

            JudgementReport report = await new MassJudgePipeline(source, options, logger)
                .RunAsync(Provenance.FromDataset(dataset, run, inputs), cancellationToken)
                .ConfigureAwait(false);

            // A per-model job of a multi-job execution only saves its answers in the checkpoints;
            // the final job writes the file once every model is done.
            if (ExecutionSetup.IsModelJob(args))
            {
                return null;
            }

            string outputPath = run.OutputFile(OutputLayout.JudgementsFolder, dataset.Name, "judged");
            await JsonReportWriter.WriteAsync(report, outputPath, cancellationToken).ConfigureAwait(false);

            JudgingSummary.Print(report);
            logger.Info($"Judgements written to {outputPath}");

            int placeholders = report.ModelDescriptions.Count(entry => entry.Value.HasPlaceholders);
            if (placeholders > 0)
            {
                logger.Warning(
                    $"{placeholders} model description(s) still hold placeholders. Fill them in in the judgement file " +
                    "before running compute-statistics.");
            }

            return outputPath;
        }
    }
}
