namespace CodeReadabilityLib.Checkpointing
{
    /// <summary>
    /// The execution stopped before finishing, but everything done so far is saved: it can
    /// be continued with <c>resume</c>.
    /// </summary>
    public sealed class ResumableStopException : Exception
    {
        /// <summary>
        /// The checkpoint folder to pass to <c>resume</c>.
        /// </summary>
        public string CheckpointFolder { get; }

        public ResumableStopException(string message, string checkpointFolder, Exception? innerException = null)
            : base(message, innerException)
        {
            CheckpointFolder = checkpointFolder;
        }
    }
}
