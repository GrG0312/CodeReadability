namespace CodeReadabilityLib.Storage.Layout
{
    /// <summary>
    /// The folder of one execution, with one subfolder per step.
    /// </summary>
    public sealed class RunFolder
    {
        /// <summary>
        /// The run folder's full path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The run folder's name, such as <c>run_20261015_140233</c>.
        /// </summary>
        public string Name => System.IO.Path.GetFileName(Path);

        public RunFolder(string path)
        {
            Path = path;
        }

        /// <summary>
        /// The path of an output file in a step subfolder, creating the subfolder.
        /// </summary>
        ///
        /// <param name="stepFolder">
        /// One of the step folder names on <see cref="OutputLayout"/>.
        /// </param>
        ///
        /// <param name="baseName">
        /// The dataset name without its ID, such as <c>bw-dataset</c>.
        /// </param>
        ///
        /// <param name="suffix">
        /// The step suffix, such as <c>judged</c>.
        /// </param>
        public string OutputFile(string stepFolder, string baseName, string suffix)
        {
            string directory = System.IO.Path.Combine(Path, stepFolder);
            Directory.CreateDirectory(directory);
            return System.IO.Path.Combine(directory, $"{baseName}_{suffix}.json");
        }
    }
}
