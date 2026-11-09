namespace CodeReadabilityLib.Logging.Builtin
{
    /// <summary>
    /// A logger that discards everything.
    /// Useful as a default in tests.
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="NullLogger"/>.
        /// </summary>
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger() { }

        public void Log(LogLevel level, string message, Exception? exception = null) { }
    }
}
