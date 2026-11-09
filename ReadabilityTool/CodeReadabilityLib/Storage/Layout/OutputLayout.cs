namespace CodeReadabilityLib.Storage.Layout
{
    /// <summary>
    /// Where the tool keeps everything it writes: the output root with one run folder per
    /// execution, and the <c>datasets</c> and <c>checkpoints</c> folders next to it.
    /// </summary>
    public sealed class OutputLayout
    {
        /// <summary>
        /// The output root's folder name when none is given.
        /// </summary>
        public const string DefaultRootName = "output";

        /// <summary>
        /// The step subfolder holding judgement files.
        /// </summary>
        public const string JudgementsFolder = "judgements";

        /// <summary>
        /// The step subfolder holding metrics files.
        /// </summary>
        public const string MetricsFolder = "metrics";

        /// <summary>
        /// The step subfolder holding statistics files.
        /// </summary>
        public const string StatisticsFolder = "statistics";

        /// <summary>
        /// The folder that holds every run folder.
        /// </summary>
        public string Root { get; }

        /// <summary>
        /// The tool's dataset copies, kept next to the output root.
        /// </summary>
        public string DatasetsDirectory { get; }

        /// <summary>
        /// Resume data of unfinished executions, kept next to the output root.
        /// </summary>
        public string CheckpointsDirectory { get; }

        /// <param name="root">
        /// The output root, or <see langword="null"/> for an <c>output</c> folder in the
        /// current directory.
        /// </param>
        public OutputLayout(string? root = null)
        {
            Root = Path.GetFullPath(root ?? Path.Combine(Directory.GetCurrentDirectory(), DefaultRootName));

            // Siblings of the root rather than children, so that moving the output with
            // --output moves the datasets and checkpoints along with it.
            string parent = Path.GetDirectoryName(Root) ?? Root;
            DatasetsDirectory = Path.Combine(parent, "datasets");
            CheckpointsDirectory = Path.Combine(parent, "checkpoints");
        }

        /// <summary>
        /// Creates a new run folder named after <paramref name="startedAt"/>.
        /// </summary>
        ///
        /// <remarks>
        /// Two executions started in the same second would collide, so a counter is
        /// appended rather than letting the second overwrite the first.
        /// </remarks>
        public RunFolder CreateRun(DateTime startedAt)
        {
            string baseName = $"run_{startedAt:yyyyMMdd_HHmmss}";
            string path = Path.Combine(Root, baseName);

            for (int counter = 2; Directory.Exists(path); counter++)
            {
                path = Path.Combine(Root, $"{baseName}_{counter}");
            }

            Directory.CreateDirectory(path);
            return new RunFolder(path);
        }
    }
}
