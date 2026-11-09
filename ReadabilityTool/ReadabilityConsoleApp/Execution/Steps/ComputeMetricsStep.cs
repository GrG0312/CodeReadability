using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Datasets;
using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Pipelines.ComputeMetrics;
using CodeReadabilityLib.Reporting;
using CodeReadabilityLib.Reporting.MetricResults;
using CodeReadabilityLib.Sources;
using CodeReadabilityLib.Storage.DatasetIdentity;
using CodeReadabilityLib.Storage.Layout;
using CodeReadabilityLib.Storage.Tracking;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Execution;

namespace ReadabilityConsoleApp.Execution.Steps
{
    /// <summary>
    /// The metrics step, shared by <c>compute-metrics</c> and <c>run-pipeline</c>.
    /// </summary>
    public static class ComputeMetricsStep
    {
        /// <param name="modelDirectory">
        /// Required when <paramref name="kinds"/> includes LLM-based metrics.
        /// </param>
        ///
        /// <returns>
        /// The written metrics file, or <see langword="null"/> in a per-model job.
        /// </returns>
        public static async Task<string?> RunAsync(
            CommandLine args,
            ILogger logger,
            ResolvedDataset dataset,
            RunFolder run,
            SupportedLanguage language,
            string kinds,
            string? modelDirectory,
            IReadOnlyDictionary<string, ModelDescription> descriptions,
            string? descriptionPath,
            CheckpointStore checkpoints,
            CancellationToken cancellationToken)
        {
            bool llm = MetricKinds.IncludesLlm(kinds);

            ComputeMetricsOptions options = new ComputeMetricsOptions
            {
                Language = language,
                Kinds = kinds,
                ModelDirectory = llm ? modelDirectory : null,
                ServerTemplate = llm ? ServerOptionsReader.Read(args) : null,
                Descriptions = descriptions,
                Checkpoints = checkpoints,
                OnlyModel = args.Optional("only-model"),
                Concurrency = args.Int("concurrency", 1),
                ThinkingTokenBudget = SharedOptions.ThinkingBudget(args)
            };

            Dictionary<string, string> inputs = new Dictionary<string, string>();
            if (llm && modelDirectory is not null)
            {
                inputs["modelDirectory"] = modelDirectory;
            }
            if (llm && descriptionPath is not null)
            {
                inputs["modelDescriptions"] = descriptionPath;
            }

            DatasetSampleSource source = new DatasetSampleSource(dataset.Path, language, DatasetLoaderFactory.Create(dataset.Path));

            MetricsReport report = await new ComputeMetricsPipeline(source, options, logger)
                .RunAsync(Provenance.FromDataset(dataset, run, inputs), cancellationToken)
                .ConfigureAwait(false);

            // A per-model job of a multi-job execution only saves its answers in the checkpoints;
            // the final job writes the file once every model is done.
            if (ExecutionSetup.IsModelJob(args))
            {
                return null;
            }

            string outputPath = run.OutputFile(OutputLayout.MetricsFolder, dataset.Name, "metrics");
            await JsonReportWriter.WriteAsync(report, outputPath, cancellationToken).ConfigureAwait(false);

            logger.Info(
                $"Metrics ({kinds}) for {report.TotalSnippets} snippet(s): {report.AlgorithmicMetrics.Count} algorithmic, " +
                $"{report.LlmMetrics.Count} LLM-based by {report.Graders.Count} model(s), {report.Failures.Count} grading failure(s).");
            logger.Info($"Metrics written to {outputPath}");

            return outputPath;
        }
    }
}
