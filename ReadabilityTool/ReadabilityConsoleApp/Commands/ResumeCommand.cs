using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using ReadabilityConsoleApp.Cli;

namespace ReadabilityConsoleApp.Commands
{
    /// <summary>
    /// Continues an execution that stopped before finishing, from its checkpoint folder.
    /// </summary>
    ///
    /// <remarks>
    /// Reads the execution's plan, so no options are given again and the same settings are
    /// guaranteed. The original command then runs as usual: its up-front checks run again,
    /// finished work is skipped, and the results go into the original run folder. A whole
    /// pipeline is completed, not only the step that stopped.
    /// </remarks>
    public sealed class ResumeCommand : ICommand
    {
        private static readonly HashSet<string> Resumable = ["mass-judge", "compute-metrics", "run-pipeline"];

        private readonly ILogger _logger;

        public ResumeCommand(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default)
        {
            CheckpointStore store = CheckpointStore.Open(Path.GetFullPath(args.Require("checkpoint")));

            if (!Resumable.Contains(store.Plan.Command))
            {
                throw new RequirementsNotMetException("resume", [$"'{store.Plan.Command}' executions cannot be resumed."]);
            }

            _logger.Info($"Resuming {store.Plan.Command} (started {store.Plan.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC) from {store.Folder}");

            Dictionary<string, string> jobOptions = new Dictionary<string, string>();
            if (args.Optional("only-model") is string onlyModel)
            {
                jobOptions["only-model"] = onlyModel;
            }

            CommandLine saved = CommandLine.ForResume(store, jobOptions);
            ICommand command = Program.ResolveCommand(saved.Command, _logger)
                ?? throw new RequirementsNotMetException("resume", [$"'{saved.Command}' is not a command."]);

            return await command.ExecuteAsync(saved, cancellationToken).ConfigureAwait(false);
        }
    }
}
