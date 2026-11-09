namespace ReadabilityConsoleApp.Cluster
{
    /// <summary>
    /// One kind of GPU on the NIPG cluster, and the partition that offers it.
    /// </summary>
    public sealed record GpuTier
    {
        /// <summary>
        /// The memory of one GPU, in GB.
        /// </summary>
        public required int MemoryGb { get; init; }

        /// <summary>
        /// The tier's name, as used in a model description's <c>gpu</c> entry, such as
        /// <c>vram24</c>.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// The SLURM constraint needed on top of the partition, or <see langword="null"/> when
        /// the partition alone guarantees the memory. Every node has exactly one <c>vramN</c>
        /// feature, so a constraint lists every feature that is large enough.
        /// </summary>
        public string? Constraint { get; init; }

        public required string Partition { get; init; }

        /// <summary>
        /// The most GPUs of this kind one job may request.
        /// </summary>
        public required int MaxGpus { get; init; }

        /// <summary>
        /// The partition's wall limit, in SLURM's format.
        /// </summary>
        public required string TimeLimit { get; init; }

        /// <summary>
        /// The cluster's tiers, smallest first, from the admins' rules file
        /// (<c>/nas/containers/AGENTS.md</c>, checked 2026-10). Update them here when it
        /// changes.
        /// </summary>
        public static IReadOnlyList<GpuTier> All { get; } =
        [
            new GpuTier { MemoryGb = 8, Name = "vram8", Partition = "small", MaxGpus = 2, TimeLimit = "1-00:00:00" },
            new GpuTier { MemoryGb = 11, Name = "vram11", Partition = "medium", MaxGpus = 4, TimeLimit = "2-00:00:00" },
            new GpuTier { MemoryGb = 16, Name = "vram16", Constraint = "vram16|vram24", Partition = "medium", MaxGpus = 4, TimeLimit = "2-00:00:00" },
            new GpuTier { MemoryGb = 24, Name = "vram24", Partition = "large", MaxGpus = 2, TimeLimit = "2-00:00:00" },
            new GpuTier { MemoryGb = 40, Name = "vram40", Partition = "extralarge", MaxGpus = 8, TimeLimit = "2-00:00:00" }
        ];
    }
}
