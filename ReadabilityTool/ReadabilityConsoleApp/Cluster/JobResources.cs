namespace ReadabilityConsoleApp.Cluster
{
    /// <summary>
    /// What one SLURM job asks for.
    /// </summary>
    public sealed record JobResources
    {
        /// <summary>
        /// The GPU kind, or <see langword="null"/> for a job that needs no GPU.
        /// </summary>
        public GpuTier? Tier { get; init; }

        public int Gpus { get; init; }

        public required int Cpus { get; init; }

        public required int MemoryGb { get; init; }

        public required string Partition { get; init; }

        public required string TimeLimit { get; init; }

        /// <summary>
        /// A short description for the submission log, such as <c>2 x vram24 (large)</c>.
        /// </summary>
        public string Label => Tier is null ? $"no GPU ({Partition})" : $"{Gpus} x {Tier.Name} ({Partition})";

        /// <summary>
        /// A job that needs no GPU: assembling files and computing statistics.
        /// </summary>
        public static JobResources CpuOnly { get; } = new JobResources
        {
            Cpus = 8,
            MemoryGb = 16,
            Partition = "small",
            TimeLimit = "0-06:00:00"
        };
    }
}
