using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Preflight.Basics;
using CodeReadabilityLib.Reporting.MetricResults;
using CodeReadabilityLib.Storage.DatasetIdentity;
using CodeReadabilityLib.Storage.Layout;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Execution;
using ReadabilityConsoleApp.Execution.Checks;
using ReadabilityConsoleApp.Execution.Steps;

namespace ReadabilityConsoleApp.Commands
{
    /// <summary>
    /// Computes the algorithmic and/or LLM-based readability metrics of every snippet of a
    /// dataset and writes them to a metrics file.
    /// </summary>
    public sealed class ComputeMetricsCommand : ICommand
    {
        private readonly ILogger _logger;

        public ComputeMetricsCommand(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default)
        {
            string datasetPath = ExecutionSetup.DatasetToCheck(args);
            string kinds = SharedOptions.MetricKindsOf(args);
            SupportedLanguage language = LanguageRegistry.FromName(args.Require("language"));
            OutputLayout layout = new OutputLayout(args.Optional("output"));

            RequirementProblems problems = new RequirementProblems();
            ExecutionSetup.CheckResume(args, problems);
            EnvironmentChecks.Dataset(datasetPath, requireScores: false, problems);

            string? modelDirectory = null;
            string? descriptionPath = null;
            Dictionary<string, ModelDescription> descriptions = new Dictionary<string, ModelDescription>();

            if (MetricKinds.IncludesLlm(kinds))
            {
                modelDirectory = Path.GetFullPath(args.Require("model-dir"));
                UpfrontChecks.ModelSide(args, modelDirectory, problems);
                (descriptionPath, descriptions) = UpfrontChecks.Descriptions(args, modelDirectory, problems);
            }

            UpfrontChecks.Writable(layout, problems);
            problems.ThrowIfAny("compute-metrics");

            ExecutionSetup setup = ExecutionSetup.Start(args, "compute-metrics", layout, _logger);

            await setup.RunAsync(args, _logger, () => ComputeMetricsStep.RunAsync(
                args, _logger, setup.Dataset, setup.Run, language, kinds, modelDirectory, descriptions, descriptionPath, setup.Checkpoints, cancellationToken))
                .ConfigureAwait(false);

            return 0;
        }
    }
}
