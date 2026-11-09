namespace ReadabilityConsoleApp.Cluster
{
    /// <summary>
    /// One SLURM job to submit: what the tool runs in it, and what it asks for.
    /// </summary>
    public sealed record ClusterJob
    {
        /// <summary>
        /// The job name, as shown by <c>squeue</c>.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// The tool's arguments inside the job, such as <c>resume --checkpoint …</c>.
        /// </summary>
        public required IReadOnlyList<string> ToolArguments { get; init; }

        public required JobResources Resources { get; init; }

        /// <summary>
        /// Whether the job starts only after every earlier job of the submission finished
        /// successfully: the final job of a <c>multi-job</c> execution.
        /// </summary>
        public bool AfterAllOthers { get; init; }

        /// <summary>
        /// A short description for the submission log.
        /// </summary>
        public required string Description { get; init; }
    }
}
