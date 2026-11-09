using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Preflight.Basics;
using CodeReadabilityLib.Storage.DatasetIdentity;
using CodeReadabilityLib.Storage.Layout;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Execution;
using ReadabilityConsoleApp.Execution.Checks;
using ReadabilityConsoleApp.Execution.Steps;

namespace ReadabilityConsoleApp.Commands
{
    /// <summary>
    /// Scores a labelled dataset with every GGUF model in a directory and writes every
    /// answer to a judgement file. Computes no statistics.
    /// </summary>
    public sealed class MassJudgeCommand : ICommand
    {
        private readonly ILogger _logger;

        public MassJudgeCommand(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default)
        {
            string datasetPath = ExecutionSetup.DatasetToCheck(args);
            string modelDirectory = Path.GetFullPath(args.Require("model-dir"));
            SupportedLanguage language = LanguageRegistry.FromName(args.Optional("language") ?? "python");
            SharedOptions.Strategies(args);
            OutputLayout layout = new OutputLayout(args.Optional("output"));

            RequirementProblems problems = new RequirementProblems();
            ExecutionSetup.CheckResume(args, problems);
            DatasetSummary? dataset = EnvironmentChecks.Dataset(datasetPath, requireScores: true, problems);
            string[] models = UpfrontChecks.ModelSide(args, modelDirectory, problems);
            (string? descriptionPath, Dictionary<string, ModelDescription> descriptions) = UpfrontChecks.Descriptions(args, modelDirectory, problems);
            UpfrontChecks.Writable(layout, problems);
            problems.ThrowIfAny("mass-judge");

            string[] toCheck = RemainingWork.Models(args, models, descriptions, judges: true, grades: false);
            await UpfrontChecks.ModelsAsync(args, _logger, toCheck, descriptions, dataset, language.Language.ToString(), problems, cancellationToken)
                .ConfigureAwait(false);
            problems.ThrowIfAny("mass-judge");

            _logger.Info(descriptionPath is null
                ? "No model description file found; every model gets placeholder descriptions."
                : $"Model descriptions: {descriptionPath}");

            ExecutionSetup setup = ExecutionSetup.Start(args, "mass-judge", layout, _logger);

            await setup.RunAsync(args, _logger, () => MassJudgeStep.RunAsync(
                args, _logger, setup.Dataset, setup.Run, language, modelDirectory, descriptions, descriptionPath, setup.Checkpoints, cancellationToken))
                .ConfigureAwait(false);

            return 0;
        }
    }
}
