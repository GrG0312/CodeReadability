using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Analysis.Inputs;
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
    /// The whole pipeline in one run folder: <c>mass-judge</c> and <c>compute-metrics</c> on
    /// one labelled dataset, then <c>compute-statistics</c> on their outputs.
    /// </summary>
    ///
    /// <remarks>
    /// Every requirement of all three steps is checked before the first one starts,
    /// including a complete model description file, without which compute-statistics could
    /// not run.
    /// </remarks>
    public sealed class RunPipelineCommand : ICommand
    {
        private const string Name = "run-pipeline";

        private readonly ILogger _logger;

        public RunPipelineCommand(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default)
        {
            string datasetPath = ExecutionSetup.DatasetToCheck(args);
            string modelDirectory = Path.GetFullPath(args.Require("model-dir"));
            SupportedLanguage language = LanguageRegistry.FromName(args.Require("language"));
            string kinds = SharedOptions.MetricKindsOf(args);
            SharedOptions.Strategies(args);
            SharedOptions.Analysis(args);
            OutputLayout layout = new OutputLayout(args.Optional("output"));

            RequirementProblems problems = new RequirementProblems();
            ExecutionSetup.CheckResume(args, problems);
            DatasetSummary? dataset = EnvironmentChecks.Dataset(datasetPath, requireScores: true, problems);
            string[] models = UpfrontChecks.ModelSide(args, modelDirectory, problems);
            (string? descriptionPath, Dictionary<string, ModelDescription> descriptions) = UpfrontChecks.Descriptions(args, modelDirectory, problems);

            if (descriptionPath is null && !problems.Any)
            {
                problems.Add(
                    $"No model description file found (looked for {ModelDescriptionFile.DefaultFileName} in the model folder). " +
                    "run-pipeline needs one describing every model, because compute-statistics cannot run on placeholders.");
            }
            else if (descriptionPath is not null)
            {
                EnvironmentChecks.DescriptionsComplete(descriptions, models, problems);
            }

            UpfrontChecks.Writable(layout, problems);
            problems.ThrowIfAny(Name);

            string[] toCheck = RemainingWork.Models(args, models, descriptions, judges: true, grades: MetricKinds.IncludesLlm(kinds));
            await UpfrontChecks.ModelsAsync(args, _logger, toCheck, descriptions, dataset, language.Language.ToString(), problems, cancellationToken)
                .ConfigureAwait(false);
            problems.ThrowIfAny(Name);

            ExecutionSetup setup = ExecutionSetup.Start(args, Name, layout, _logger);
            CheckpointStore checkpoints = setup.Checkpoints;

            await setup.RunAsync(args, _logger, async () =>
            {
                if (ExecutionSetup.IsModelJob(args))
                {
                    // One model of a multi-job execution: judge and grade with it, nothing more.
                    _logger.Info("=== Step 1/3: mass-judge (one model) ===");
                    await MassJudgeStep.RunAsync(
                        args, _logger, setup.Dataset, setup.Run, language, modelDirectory, descriptions, descriptionPath, checkpoints, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.Info("=== Step 2/3: compute-metrics (one model) ===");
                    await ComputeMetricsStep.RunAsync(
                        args, _logger, setup.Dataset, setup.Run, language, kinds, modelDirectory, descriptions, descriptionPath, checkpoints, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                string judgements = await StepAsync(checkpoints, "mass-judge", "1/3", () => MassJudgeStep.RunAsync(
                    args, _logger, setup.Dataset, setup.Run, language, modelDirectory, descriptions, descriptionPath, checkpoints, cancellationToken))
                    .ConfigureAwait(false);

                string metrics = await StepAsync(checkpoints, "compute-metrics", "2/3", () => ComputeMetricsStep.RunAsync(
                    args, _logger, setup.Dataset, setup.Run, language, kinds, modelDirectory, descriptions, descriptionPath, checkpoints, cancellationToken))
                    .ConfigureAwait(false);

                _logger.Info("=== Step 3/3: compute-statistics ===");
                StatisticsInputs inputs = StatisticsInputs.Load(judgements, metrics);
                await ComputeStatisticsStep.RunAsync(args, _logger, inputs, setup.Run, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

            _logger.Info($"Pipeline finished. Everything is in {setup.Run.Path}");
            return 0;
        }

        /// <summary>
        /// Runs one step, unless a resumed execution already finished it, and records its
        /// output file in the plan.
        /// </summary>
        private async Task<string> StepAsync(CheckpointStore checkpoints, string step, string position, Func<Task<string?>> run)
        {
            if (checkpoints.Plan.StepOutputs.TryGetValue(step, out string? saved) && File.Exists(saved))
            {
                _logger.Info($"=== Step {position}: {step} === already finished: {saved}");
                return saved;
            }

            _logger.Info($"=== Step {position}: {step} ===");
            string output = await run().ConfigureAwait(false)
                ?? throw new InvalidOperationException($"{step} wrote no output file.");

            checkpoints.UpdatePlan(plan => plan with
            {
                StepOutputs = new Dictionary<string, string>(plan.StepOutputs) { [step] = output }
            });

            return output;
        }
    }
}
