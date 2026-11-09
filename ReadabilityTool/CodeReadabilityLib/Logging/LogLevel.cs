namespace CodeReadabilityLib.Logging
{
    /// <summary>
    /// Represents the severity level of a log message.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Represents a debug-level log message, typically used for development and troubleshooting purposes.
        /// </summary>
        Debug,

        /// <summary>
        /// Represents an informational log message, providing general information about the application's execution.
        /// </summary>
        Info,

        /// <summary>
        /// Represents a warning-level log message, indicating a potential issue or unexpected behavior that may require attention.
        /// </summary>
        Warning,

        /// <summary>
        /// Represents an error-level log message, indicating a significant problem or failure that may impact the application's functionality.
        /// </summary>
        Error
    }
}
