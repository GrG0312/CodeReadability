using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Preflight;
using CodeReadabilityLib.Storage.Layout;
using ReadabilityConsoleApp.Cli;
using ReadabilityConsoleApp.Execution.Checks;
using ReadabilityConsoleApp.Execution.Steps;

namespace ReadabilityConsoleApp.Commands
{
    /// <summary>
    /// Computes every statistic of a judgement file (per-run agreement, model axes,
    /// prompting strategies, metric impact) and writes a copy of it with the statistics
    /// added. The input file is never modified.
    /// </summary>
    public sealed class ComputeStatisticsCommand : ICommand
    {
        private readonly ILogger _logger;

        public ComputeStatisticsCommand(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default)
        {
            SharedOptions.Analysis(args);
            OutputLayout layout = new OutputLayout(args.Optional("output"));

            RequirementProblems problems = new RequirementProblems();
            UpfrontChecks.Writable(layout, problems);
            problems.ThrowIfAny("compute-statistics");

            // Loading checks the inputs: same dataset, complete model descriptions.
            StatisticsInputs inputs = StatisticsInputs.Load(args.Require("judgements"), args.Require("metrics"));

            RunFolder run = layout.CreateRun(DateTime.Now);
            _logger.Info($"Run folder: {run.Path}");

            await ComputeStatisticsStep.RunAsync(args, _logger, inputs, run, cancellationToken).ConfigureAwait(false);
            return 0;
        }
    }
}
