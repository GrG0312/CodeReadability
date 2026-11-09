using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Preflight.Basics;
using CodeReadabilityLib.Preflight.Models;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Execution.Checks;
using ReadabilityConsoleApp.Printing;

namespace ReadabilityConsoleApp.Commands
{
    /// <summary>
    /// Tests every model in the model folder with a few short requests before committing to
    /// a long run, and estimates how long judging would take. Writes no files.
    /// </summary>
    public sealed class CheckModelsCommand : ICommand
    {
        /// <summary>
        /// The snippet count the estimate uses when no dataset is given.
        /// </summary>
        private const int DefaultSnippets = 100;

        private readonly ILogger _logger;

        public CheckModelsCommand(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default)
        {
            string modelDirectory = Path.GetFullPath(args.Require("model-dir"));
            SupportedLanguage language = LanguageRegistry.FromName(args.Optional("language") ?? "python");
            int strategies = SharedOptions.Strategies(args).Count;

            RequirementProblems problems = new RequirementProblems();
            string[] models = UpfrontChecks.ModelSide(args, modelDirectory, problems);
            (string? descriptionPath, Dictionary<string, ModelDescription> descriptions) = UpfrontChecks.Descriptions(args, modelDirectory, problems);
            DatasetSummary? dataset = args.Optional("dataset") is string datasetPath
                ? EnvironmentChecks.Dataset(datasetPath, requireScores: false, problems)
                : null;
            problems.ThrowIfAny("check-models");

            // Model problems are reported in the summary rather than thrown, because finding
            // them is this command's purpose.
            RequirementProblems modelProblems = new RequirementProblems();
            List<ModelCheckResult> results = await UpfrontChecks.ModelsAsync(
                args, _logger, models, descriptions, dataset, language.Language.ToString(), modelProblems, cancellationToken).ConfigureAwait(false);

            ModelCheckSummary.Print(
                results,
                dataset?.Labelled is int labelled and > 0 ? labelled : DefaultSnippets,
                strategies,
                args.Int("concurrency", 1),
                snippetsFromDataset: dataset is not null);

            PrintUndescribedModels(descriptionPath, descriptions, models);

            return modelProblems.Any ? 1 : 0;
        }

        /// <summary>
        /// Points out models tested without a description, which are run once in their
        /// default mode, so a hybrid model's thinking switch goes unchecked. Not a problem for
        /// this command, but <c>run-pipeline</c> refuses to start until every model is described.
        /// </summary>
        private static void PrintUndescribedModels(string? descriptionPath, IReadOnlyDictionary<string, ModelDescription> descriptions, IEnumerable<string> models)
        {
            List<string> undescribed = models
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Where(model => !descriptions.ContainsKey(model))
                .ToList();

            if (undescribed.Count == 0)
            {
                return;
            }

            Console.WriteLine("NOTE");
            Console.WriteLine(descriptionPath is null
                ? $"  No model description file was found ({ModelDescriptionFile.DefaultFileName} in the model folder, or --model-descriptions)."
                : $"  These models are missing from {descriptionPath}:");

            if (descriptionPath is not null)
            {
                foreach (string model in undescribed)
                {
                    Console.WriteLine($"    {model}");
                }
            }

            Console.WriteLine(descriptionPath is null
                ? "  Every model was tested once, in its default mode; a hybrid model's thinking switch was not checked."
                : "  They were tested once, in their default mode; a hybrid model's thinking switch was not checked.");
            Console.WriteLine("  run-pipeline will not start until every model is described.");
            Console.WriteLine();
        }
    }
}
