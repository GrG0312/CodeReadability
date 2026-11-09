namespace CodeReadabilityLib.Logging
{
    /// <summary>
    /// Provides extension methods for the <see cref="ILogger"/> interface to simplify logging at different log levels.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs a debug message using the provided <see cref="ILogger"/> instance.
        /// </summary>
        /// 
        /// <param name="logger">
        /// The <see cref="ILogger"/> instance used to log the message.
        /// </param>
        /// 
        /// <param name="message">
        /// The message to be logged.
        /// </param>
        /// 
        /// <param name="exception">
        /// An optional exception associated with the log message. If provided, it will be included in the log entry.
        /// </param>
        public static void Debug(this ILogger logger, string message, Exception? exception = null)
        {
            logger.Log(LogLevel.Debug, message, exception);
        }

        /// <summary>
        /// Logs an informational message using the provided <see cref="ILogger"/> instance.
        /// </summary>
        /// 
        /// <param name="logger">
        /// The <see cref="ILogger"/> instance used to log the message.
        /// </param>
        /// 
        /// <param name="message">
        /// The message to be logged.
        /// </param>
        /// 
        /// <param name="exception">
        /// An optional exception associated with the log message. If provided, it will be included in the log entry.
        /// </param>
        public static void Info(this ILogger logger, string message, Exception? exception = null)
        {
            logger.Log(LogLevel.Info, message, exception);
        }

        /// <summary>
        /// Logs a warning message using the provided <see cref="ILogger"/> instance.
        /// </summary>
        /// 
        /// <param name="logger">
        /// The <see cref="ILogger"/> instance used to log the message.
        /// </param>
        /// 
        /// <param name="message">
        /// The message to be logged.
        /// </param>
        /// 
        /// <param name="exception">
        /// An optional exception associated with the log message. If provided, it will be included in the log entry.
        /// </param>
        public static void Warning(this ILogger logger, string message, Exception? exception = null)
        {
            logger.Log(LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// Logs an error message using the provided <see cref="ILogger"/> instance.
        /// </summary>
        /// 
        /// <param name="logger">
        /// The <see cref="ILogger"/> instance used to log the message.
        /// </param>
        /// 
        /// <param name="message">
        /// The message to be logged.
        /// </param>
        /// 
        /// <param name="exception">
        /// An optional exception associated with the log message. If provided, it will be included in the log entry.
        /// </param>
        public static void Error(this ILogger logger, string message, Exception? exception = null)
        {
            logger.Log(LogLevel.Error, message, exception);
        }
    }
}
