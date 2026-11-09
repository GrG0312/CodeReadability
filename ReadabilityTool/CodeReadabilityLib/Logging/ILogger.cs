namespace CodeReadabilityLib.Logging
{
    /// <summary>
    /// Represents a logger that can log messages with different log levels.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the specified log level and an optional exception.
        /// </summary>
        /// 
        /// <param name="level">
        /// The log level of the message. This parameter determines the severity of the log entry and can be one of the following values:
        /// </param>
        /// 
        /// <param name="message">
        /// The message to be logged. This parameter should contain the information you want to record in the log.
        /// </param>
        /// 
        /// <param name="exception">
        /// An optional exception associated with the log entry. 
        /// If an exception is provided, it will be included in the log output to provide additional context about the error or issue being logged.
        /// </param>
        public void Log(LogLevel level, string message, Exception? exception = null);
    }
}
