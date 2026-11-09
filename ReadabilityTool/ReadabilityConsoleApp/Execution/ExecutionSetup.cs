using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Storage.DatasetIdentity;
using CodeReadabilityLib.Storage.Layout;
using ReadabilityConsoleApp.Cli;

namespace ReadabilityConsoleApp.Execution
{
    /// <summary>
    /// Sets up an execution that reads a dataset: its dataset copy, its run folder and its
    /// checkpoint store, either new or taken over from the execution being resumed.
    /// </summary>
    public sealed class ExecutionSetup
    {
        public required ResolvedDataset Dataset { get; init; }

        public required RunFolder Run { get; init; }

        public required CheckpointStore Checkpoints { get; init; }

        /// <summary>
        /// The dataset file the up-front checks should look at: the saved copy when resuming,
        /// otherwise <c>--dataset</c>.
        /// </summary>
        public static string DatasetToCheck(CommandLine args)
        {
            return args.Resume?.Plan.DatasetPath ?? args.Require("dataset");
        }

        /// <summary>
        /// Checks, when resuming, that the saved dataset copy is unchanged.
        /// </summary>
        public static void CheckResume(CommandLine args, RequirementProblems problems)
        {
            if (args.Resume?.Plan is not ExecutionPlan plan || plan.DatasetPath is null)
            {
                return;
            }

            if (!File.Exists(plan.DatasetPath))
            {
                problems.Add($"The dataset copy of the execution being resumed is missing: {plan.DatasetPath}");
            }
            else if (plan.DatasetSha256 is not null && CheckpointStore.Sha256Of(plan.DatasetPath) != plan.DatasetSha256)
            {
                problems.Add($"The dataset copy changed since the execution started: {plan.DatasetPath}. Start a new execution.");
            }
        }

        /// <summary>
        /// Creates the run folder and checkpoint store of a new execution, or reopens those of
        /// the execution being resumed. Called only after every up-front check passed.
        /// </summary>
        public static ExecutionSetup Start(CommandLine args, string command, OutputLayout layout, ILogger logger)
        {
            if (args.Resume is CheckpointStore store)
            {
                ResolvedDataset copy = new DatasetCatalog(layout.DatasetsDirectory, logger).Resolve(store.Plan.DatasetPath!, DateTime.Now);
                RunFolder resumed = new RunFolder(store.Plan.RunFolder);
                Directory.CreateDirectory(resumed.Path);
                logger.Info($"Resuming {store.Plan.Command} in {resumed.Path}");
                return new ExecutionSetup { Dataset = copy, Run = resumed, Checkpoints = store };
            }

            return Create(args, command, layout, logger);
        }

        /// <summary>
        /// Creates a new execution: the dataset copy, the run folder and the checkpoint store
        /// with the plan. <c>submit</c> calls this on the login node, so that every job of
        /// the execution shares it.
        /// </summary>
        public static ExecutionSetup Create(CommandLine args, string command, OutputLayout layout, ILogger logger)
        {
            DateTime startedAt = DateTime.Now;
            ResolvedDataset dataset = new DatasetCatalog(layout.DatasetsDirectory, logger).Resolve(args.Require("dataset"), startedAt);
            RunFolder run = layout.CreateRun(startedAt);
            logger.Info($"Run folder: {run.Path}");

            string checkpointsRoot = Path.GetFullPath(args.Optional("checkpoint-dir") ?? layout.CheckpointsDirectory);
            CheckpointStore checkpoints = CheckpointStore.Create(checkpointsRoot, run.Name, new ExecutionPlan
            {
                Command = command,
                Options = args.Options
                    .Where(option => !PerJobOptions.Contains(option.Key))
                    .ToDictionary(option => option.Key, option => option.Value),
                RunFolder = run.Path,
                DatasetPath = dataset.Path,
                DatasetId = dataset.Id,
                DatasetSha256 = CheckpointStore.Sha256Of(dataset.Path)
            });
            logger.Info($"Checkpoints: {checkpoints.Folder}");

            return new ExecutionSetup { Dataset = dataset, Run = run, Checkpoints = checkpoints };
        }

        /// <summary>
        /// Options that belong to one job or to the submission, not to the execution, and are
        /// therefore not saved in its plan.
        /// </summary>
        private static readonly HashSet<string> PerJobOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "only-model", "job-mode", "checkpoint"
        };

        /// <summary>
        /// Whether this job works on one model of a <c>multi-job</c> execution, which leaves
        /// the assembling of the output files and the clean-up to the final job.
        /// </summary>
        public static bool IsModelJob(CommandLine args)
        {
            return args.Optional("only-model") is not null;
        }

        /// <summary>
        /// Runs the execution's work. On success the checkpoints are deleted, unless this is
        /// a per-model job; on a stop or a cancellation they are kept, and how to resume is
        /// logged.
        /// </summary>
        public async Task RunAsync(CommandLine args, ILogger logger, Func<Task> work)
        {
            try
            {
                await work().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                LogResumeHint(logger);
                throw;
            }
            catch (ResumableStopException)
            {
                LogResumeHint(logger);
                throw;
            }

            if (IsModelJob(args))
            {
                logger.Info($"Model {args.Optional("only-model")} done; the final job assembles the output files.");
                return;
            }

            Checkpoints.Delete();
        }

        private void LogResumeHint(ILogger logger)
        {
            logger.Warning($"Stopped before finishing; everything done so far is saved. Continue with:");
            logger.Warning($"  ReadabilityConsoleApp resume --checkpoint \"{Checkpoints.Folder}\"");
        }
    }
}
