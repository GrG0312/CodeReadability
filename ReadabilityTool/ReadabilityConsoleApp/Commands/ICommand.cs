using ReadabilityConsoleApp.Cli;

namespace ReadabilityConsoleApp.Commands
{
    /// <summary>
    /// One executable verb of the command-line tool.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Runs the command.
        /// </summary>
        ///
        /// <param name="args">
        /// The parsed command line, from which the command reads its own options.
        /// </param>
        ///
        /// <returns>
        /// The process exit code: 0 on success, non-zero on failure.
        /// </returns>
        public Task<int> ExecuteAsync(CommandLine args, CancellationToken cancellationToken = default);
    }
}
