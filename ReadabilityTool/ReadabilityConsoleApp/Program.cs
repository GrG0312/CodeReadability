using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Preflight;
using ReadabilityConsoleApp.Cli;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Logging.Builtin;
using ReadabilityConsoleApp.Commands;

namespace ReadabilityConsoleApp
{
    public static class Program
    {
        /// <summary>
        /// Exit code conventionally used for termination by Ctrl+C.
        /// </summary>
        private const int CancelledExitCode = 130;

        /// <summary>
        /// Exit code when a requirement is not met and nothing was started.
        /// </summary>
        private const int RequirementsNotMetExitCode = 2;

        /// <summary>
        /// Exit code when an execution stopped early but can be resumed (EX_TEMPFAIL), which
        /// job scripts use to resubmit the execution as <c>resume</c>.
        /// </summary>
        public const int ResumableExitCode = 75;

        public static async Task<int> Main(string[] args)
        {
            using CancellationTokenSource cancellation = new CancellationTokenSource();

            // The comparison output uses ρ, Δ and χ², which the default Windows console code
            // page cannot display.
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Handle Ctrl+C ourselves so that cancellation unwinds through the pipelines and
            // every llama-server child process is shut down, instead of being orphaned.
            Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                Console.Error.WriteLine("Cancellation requested, shutting down...");
                cancellation.Cancel();
            };

            CommandLine parsed = CommandLine.Parse(args);
            ILogger logger = new ConsoleLogger(parsed.Verbose ? LogLevel.Debug : LogLevel.Info);

            try
            {
                ICommand? command = ResolveCommand(parsed.Command, logger);

                if (command is null)
                {
                    HelpText.Print();
                    return 0;
                }

                return await command.ExecuteAsync(parsed, cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.Warning("Cancelled.");
                return CancelledExitCode;
            }
            catch (RequirementsNotMetException ex)
            {
                logger.Error(ex.Message);
                return RequirementsNotMetExitCode;
            }
            catch (ResumableStopException ex)
            {
                logger.Error(ex.Message);
                return ResumableExitCode;
            }
            catch (ArgumentException ex)
            {
                // A usage mistake, so show what the correct usage is.
                logger.Error(ex.Message);
                Console.Error.WriteLine();
                HelpText.Print();
                return 1;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, parsed.Verbose ? ex : null);
                return 1;
            }
        }

        /// <summary>
        /// Maps a verb onto its implementation.
        /// </summary>
        ///
        /// <returns>
        /// The command to run, or <see langword="null"/> when the user asked for help.
        /// </returns>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the verb is not recognised.
        /// </exception>
        internal static ICommand? ResolveCommand(string command, ILogger logger)
        {
            return command switch
            {
                "resume" => new ResumeCommand(logger),
                "submit" => new SubmitCommand(logger),
                "mass-judge" => new MassJudgeCommand(logger),
                "compute-metrics" => new ComputeMetricsCommand(logger),
                "compute-statistics" => new ComputeStatisticsCommand(logger),
                "run-pipeline" => new RunPipelineCommand(logger),
                "check-models" => new CheckModelsCommand(logger),
                "help" or "--help" or "-h" => null,
                _ => throw new ArgumentException($"Unknown command '{command}'.")
            };
        }
    }
}
