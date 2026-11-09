using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Preflight.Basics;
using CodeReadabilityLib.Reporting.MetricResults;
using CodeReadabilityLib.Storage.Layout;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Cluster;
using ReadabilityConsoleApp.Execution;
using ReadabilityConsoleApp.Execution.Checks;

namespace ReadabilityConsoleApp.Commands
{
    /// <summary>
    /// Plans the SLURM jobs for a command on the cluster's login node and writes the script
    /// that submits them. Runs only light work: the checks that need no GPU, creating the
    /// execution, and sizing each job's GPU request.
    /// </summary>
    ///
    /// <remarks>
    /// For the commands that run models on a dataset, the execution (dataset copy, run folder,
    /// checkpoint plan) is created here, and every job runs <c>resume</c> on it. In
    /// <c>multi-job</c> mode each model gets its own job, and a final job, started once
    /// every model job succeeded, assembles the output files and computes the statistics.
    /// </remarks>
    public sealed class SubmitCommand : ICommand
    {
        private const string Name = "submit";

        /// <summary>
        /// Options of the submission itself, not passed on to the submitted command.
        /// </summary>
        private static readonly string[] SubmissionOptions = ["job-mode", "script-out"];

        private readonly ILogger _logger;

        public SubmitCommand(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default)
        {
            string command = args.Positionals.FirstOrDefault()
                ?? throw new ArgumentException("submit needs the command to run, for example: submit run-pipeline --dataset ...");
            string scriptPath = Path.GetFullPath(args.Require("script-out"));
            bool multiJob = JobMode(args.Optional("job-mode"));
            CommandLine inner = args.WithCommand(command, SubmissionOptions);
            OutputLayout layout = new OutputLayout(inner.Optional("output"));

            List<ClusterJob> jobs = command switch
            {
                "mass-judge" or "compute-metrics" or "run-pipeline" => PlanExecution(inner, command, layout, multiJob),
                "check-models" => PlanCheckModels(inner),
                "resume" => PlanResume(inner),
                "compute-statistics" => [Direct(inner, command, JobResources.CpuOnly, "compute-statistics")],
                _ => throw new ArgumentException($"submit cannot run '{command}'. Use mass-judge, compute-metrics, run-pipeline, check-models, compute-statistics or resume.")
            };

            Directory.CreateDirectory(Path.GetDirectoryName(scriptPath)!);
            await File.WriteAllTextAsync(scriptPath, SubmissionScript.Build(Directory.GetCurrentDirectory(), jobs), cancellationToken)
                .ConfigureAwait(false);

            _logger.Info($"Planned {jobs.Count} job(s):");
            foreach (ClusterJob job in jobs)
            {
                _logger.Info($"  {job.Description}: {job.Resources.Label}, {job.Resources.Cpus} CPUs, {job.Resources.MemoryGb} GB, {job.Resources.TimeLimit}");
            }

            return 0;
        }

        /// <summary>
        /// Checks the command's requirements that need no GPU, creates its execution, and
        /// plans its jobs.
        /// </summary>
        private List<ClusterJob> PlanExecution(CommandLine inner, string command, OutputLayout layout, bool multiJob)
        {
            string kinds = command == "mass-judge" ? MetricKinds.AlgorithmicOnly : SharedOptions.MetricKindsOf(inner);
            bool usesModels = command != "compute-metrics" || MetricKinds.IncludesLlm(kinds);
            SharedOptions.Strategies(inner);
            SharedOptions.Analysis(inner);
            CheckLanguage(inner);

            RequirementProblems problems = new RequirementProblems();
            EnvironmentChecks.Dataset(inner.Require("dataset"), requireScores: command != "compute-metrics", problems);

            string[] models = [];
            Dictionary<string, ModelDescription> descriptions = new Dictionary<string, ModelDescription>();
            if (usesModels)
            {
                string modelDirectory = Path.GetFullPath(inner.Require("model-dir"));
                models = UpfrontChecks.ModelSide(inner, modelDirectory, problems);
                (string? descriptionPath, descriptions) = UpfrontChecks.Descriptions(inner, modelDirectory, problems);

                if (command == "run-pipeline")
                {
                    if (descriptionPath is null)
                    {
                        problems.Add($"run-pipeline needs a model description file ({ModelDescriptionFile.DefaultFileName} in the model folder, or --model-descriptions).");
                    }
                    else
                    {
                        EnvironmentChecks.DescriptionsComplete(descriptions, models, problems);
                    }
                }
            }

            UpfrontChecks.Writable(layout, problems);
            problems.ThrowIfAny(Name);

            // Sized before the execution is created, so that an impossible request stops here.
            int concurrency = inner.Int("concurrency", 1);
            List<(string ModelFile, JobResources Resources)> perModel = models
                .Select(path => (Path.GetFileNameWithoutExtension(path),
                    GpuSizing.ForModel(path, descriptions.GetValueOrDefault(Path.GetFileNameWithoutExtension(path)), concurrency)))
                .ToList();

            ExecutionSetup setup = ExecutionSetup.Create(inner, command, layout, _logger);
            string checkpoint = setup.Checkpoints.Folder;
            string runName = setup.Run.Name;

            if (perModel.Count == 0)
            {
                return [Job($"rd-{runName}", ["resume", "--checkpoint", checkpoint], JobResources.CpuOnly, $"{command} (no GPU needed)")];
            }

            if (!multiJob)
            {
                return [Job($"rd-{runName}", ["resume", "--checkpoint", checkpoint], GpuSizing.Largest(perModel.Select(m => m.Resources).ToList()), $"{command}, all models")];
            }

            List<ClusterJob> jobs = perModel
                .Select(model => Job(
                    $"rd-{runName}-{model.ModelFile}",
                    ["resume", "--checkpoint", checkpoint, "--only-model", model.ModelFile],
                    model.Resources,
                    $"{command}, model {model.ModelFile}"))
                .ToList();

            jobs.Add(Job($"rd-{runName}-final", ["resume", "--checkpoint", checkpoint], JobResources.CpuOnly, $"{command}, final assembly") with
            {
                AfterAllOthers = true
            });

            return jobs;
        }

        /// <summary>
        /// One job that continues a stopped execution, sized for its largest model.
        /// </summary>
        private static List<ClusterJob> PlanResume(CommandLine inner)
        {
            CheckpointStore store = CheckpointStore.Open(Path.GetFullPath(inner.Require("checkpoint")));
            CommandLine saved = CommandLine.ForResume(store);

            bool usesModels = saved.Command != "compute-metrics" || MetricKinds.IncludesLlm(SharedOptions.MetricKindsOf(saved));
            JobResources resources = JobResources.CpuOnly;

            if (usesModels && saved.Optional("model-dir") is string modelDirectory)
            {
                RequirementProblems problems = new RequirementProblems();
                string[] models = EnvironmentChecks.ModelFolder(Path.GetFullPath(modelDirectory), problems);
                (_, Dictionary<string, ModelDescription> descriptions) = UpfrontChecks.Descriptions(saved, Path.GetFullPath(modelDirectory), problems);
                problems.ThrowIfAny(Name);

                int concurrency = saved.Int("concurrency", 1);
                resources = GpuSizing.Largest(models
                    .Select(path => GpuSizing.ForModel(path, descriptions.GetValueOrDefault(Path.GetFileNameWithoutExtension(path)), concurrency))
                    .ToList());
            }

            return [Job($"rd-resume-{Path.GetFileName(store.Folder)}", ["resume", "--checkpoint", store.Folder], resources, $"resume {store.Plan.Command}")];
        }

        private static List<ClusterJob> PlanCheckModels(CommandLine inner)
        {
            CheckLanguage(inner);

            RequirementProblems problems = new RequirementProblems();
            if (inner.Optional("dataset") is string datasetPath)
            {
                EnvironmentChecks.Dataset(datasetPath, requireScores: false, problems);
            }

            string[] models = UpfrontChecks.ModelSide(inner, Path.GetFullPath(inner.Require("model-dir")), problems);
            (_, Dictionary<string, ModelDescription> descriptions) = UpfrontChecks.Descriptions(inner, Path.GetFullPath(inner.Require("model-dir")), problems);
            problems.ThrowIfAny(Name);

            int concurrency = inner.Int("concurrency", 1);
            JobResources resources = GpuSizing.Largest(models
                .Select(path => GpuSizing.ForModel(path, descriptions.GetValueOrDefault(Path.GetFileNameWithoutExtension(path)), concurrency))
                .ToList()) with { TimeLimit = "0-03:00:00" };

            return [Direct(inner, "check-models", resources, "check-models")];
        }

        /// <summary>
        /// A job that runs a command directly, without an execution to resume.
        /// </summary>
        private static ClusterJob Direct(CommandLine inner, string command, JobResources resources, string description)
        {
            List<string> arguments = [command];
            foreach ((string key, string value) in inner.Options)
            {
                arguments.Add(value == "true" ? $"--{key}" : $"--{key}={value}");
            }

            return Job($"rd-{command}", arguments, resources, description);
        }

        private static ClusterJob Job(string name, IReadOnlyList<string> arguments, JobResources resources, string description)
        {
            return new ClusterJob { Name = name, ToolArguments = arguments, Resources = resources, Description = description };
        }

        /// <summary>
        /// Stops here, before anything waits in the queue, when <c>--language</c> names a
        /// language the tool does not know.
        /// </summary>
        ///
        /// <exception cref="LanguageNotFoundException">
        /// Thrown when the language is not supported.
        /// </exception>
        private static void CheckLanguage(CommandLine inner)
        {
            if (inner.Optional("language") is string language)
            {
                LanguageRegistry.FromName(language);
            }
        }

        private static bool JobMode(string? mode)
        {
            return (mode ?? "one-job").Trim().ToLowerInvariant() switch
            {
                "one-job" => false,
                "multi-job" => true,
                _ => throw new ArgumentException($"--job-mode must be one-job or multi-job, got '{mode}'.")
            };
        }
    }
}
