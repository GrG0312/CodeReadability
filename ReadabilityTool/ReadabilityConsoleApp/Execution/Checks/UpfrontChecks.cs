using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Preflight.Basics;
using CodeReadabilityLib.Preflight.Models;
using CodeReadabilityLib.Storage.Layout;
using ReadabilityConsoleApp.Cli;

namespace ReadabilityConsoleApp.Execution.Checks
{
    /// <summary>
    /// The up-front requirement checks the commands assemble from, so that every command
    /// checks every requirement of every step it will run before running anything.
    /// </summary>
    public static class UpfrontChecks
    {
        /// <summary>
        /// Checks the model folder and the model server program.
        /// </summary>
        ///
        /// <returns>
        /// The model paths, sorted.
        /// </returns>
        public static string[] ModelSide(CommandLine args, string modelDirectory, RequirementProblems problems)
        {
            string[] models = EnvironmentChecks.ModelFolder(modelDirectory, problems);
            EnvironmentChecks.ServerProgram(args.Optional("llama-server-path") ?? "llama-server", problems);
            return models;
        }

        /// <summary>
        /// Every folder the tool writes to: the output root and the datasets and checkpoints
        /// folders next to it.
        /// </summary>
        public static void Writable(OutputLayout layout, RequirementProblems problems)
        {
            EnvironmentChecks.Writable([layout.Root, layout.DatasetsDirectory, layout.CheckpointsDirectory], problems);
        }

        /// <summary>
        /// Locates and loads the model description file.
        /// </summary>
        ///
        /// <returns>
        /// The file's path (or <see langword="null"/> when there is none) and its descriptions.
        /// </returns>
        public static (string? Path, Dictionary<string, ModelDescription> Descriptions) Descriptions(
            CommandLine args,
            string modelDirectory,
            RequirementProblems problems)
        {
            try
            {
                string? path = ModelDescriptionFile.Locate(args.Optional("model-descriptions"), modelDirectory);
                return (path, path is null ? new Dictionary<string, ModelDescription>() : ModelDescriptionFile.Load(path));
            }
            catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
            {
                problems.Add(ex.Message);
                return (null, new Dictionary<string, ModelDescription>());
            }
        }

        /// <summary>
        /// Tests every model with a few short requests and adds every model problem found.
        /// </summary>
        public static async Task<List<ModelCheckResult>> ModelsAsync(
            CommandLine args,
            ILogger logger,
            IReadOnlyList<string> modelPaths,
            IReadOnlyDictionary<string, ModelDescription> descriptions,
            DatasetSummary? dataset,
            string language,
            RequirementProblems problems,
            CancellationToken cancellationToken)
        {
            ModelChecker checker = new ModelChecker(
                ServerOptionsReader.Read(args),
                SharedOptions.Strategies(args),
                SharedOptions.ThinkingBudget(args),
                args.Int("concurrency", 1),
                logger);

            string sample = dataset?.SampleCode is { Length: > 0 } code ? code : BuiltInSample;
            string longest = dataset?.LongestCode is { Length: > 0 } longestCode ? longestCode : BuiltInSample;

            List<ModelCheckResult> results = new List<ModelCheckResult>();
            foreach (string modelPath in modelPaths)
            {
                string modelFile = Path.GetFileNameWithoutExtension(modelPath);
                logger.Info($"Checking model {modelFile}...");

                ModelDescription description = descriptions.GetValueOrDefault(modelFile) ?? new ModelDescription();
                ModelCheckResult result = await checker.CheckAsync(modelPath, description, sample, longest, language, cancellationToken)
                    .ConfigureAwait(false);

                results.Add(result);
                foreach (string problem in result.Problems)
                {
                    problems.Add(problem);
                }
            }

            return results;
        }

        /// <summary>
        /// The test snippet when no dataset is given.
        /// </summary>
        private const string BuiltInSample =
            "def average(values):\n" +
            "    if not values:\n" +
            "        return 0\n" +
            "    return sum(values) / len(values)";
    }
}
