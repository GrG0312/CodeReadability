namespace CodeReadabilityLib.Logging.Builtin
{
    /// <summary>
    /// Writes log entries to the console.
    /// </summary>
    public sealed class ConsoleLogger : ILogger
    {
        /// <summary>
        /// A lock object to ensure that log entries are written to the console in a thread-safe manner.
        /// </summary>
        private readonly Lock _gate = new Lock();

        /// <summary>
        /// The minimum log level that will be written to the console. Log entries with a level lower than this will be ignored.
        /// </summary>
        private readonly LogLevel _minimumLevel;

        public ConsoleLogger(LogLevel minimumLevel = LogLevel.Info)
        {
            _minimumLevel = minimumLevel;
        }

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            if (level < _minimumLevel)
            {
                return;
            }

            lock (_gate)
            {
                // Save the current console foreground color so we can restore it later
                ConsoleColor previous = Console.ForegroundColor;

                // Set the console foreground color based on the log level
                Console.ForegroundColor = level switch
                {
                    LogLevel.Debug => ConsoleColor.DarkGray,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => previous
                };

                // Write the log entry to the appropriate console stream
                TextWriter sink = level >= LogLevel.Warning ? Console.Error : Console.Out;
                sink.WriteLine($"[{DateTime.Now:HH:mm:ss}] {level,-7} {message}");

                // If an exception is provided, write its details to the console
                if (exception is not null)
                {
                    sink.WriteLine(exception);
                }

                // Restore the original console foreground color
                Console.ForegroundColor = previous;
            }
        }
    }
}
