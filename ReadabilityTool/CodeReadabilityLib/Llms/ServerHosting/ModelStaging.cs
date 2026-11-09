using CodeReadabilityLib.Logging;

namespace CodeReadabilityLib.Llms.ServerHosting
{
    /// <summary>
    /// Copies a model file to fast local storage before it is loaded, when a staging folder
    /// is configured.
    /// </summary>
    ///
    /// <remarks>
    /// On the cluster, home folders are network storage; the job script points
    /// <see cref="EnvironmentVariable"/> at the compute node's local <c>/tmp</c>. Only the
    /// model in use is staged, and it is deleted again when the model is done, so that the
    /// local disk never holds more than one model.
    /// </remarks>
    public static class ModelStaging
    {
        /// <summary>
        /// The environment variable naming the staging folder. Unset: no staging.
        /// </summary>
        public const string EnvironmentVariable = "READABILITY_MODEL_CACHE";

        /// <summary>
        /// The path to load the model from: a staged copy, or the original when no staging
        /// folder is configured.
        /// </summary>
        public static string Stage(string modelPath, ILogger logger)
        {
            if (Environment.GetEnvironmentVariable(EnvironmentVariable) is not { Length: > 0 } folder)
            {
                return modelPath;
            }

            Directory.CreateDirectory(folder);
            string staged = Path.Combine(folder, Path.GetFileName(modelPath));

            FileInfo original = new FileInfo(modelPath);
            FileInfo copy = new FileInfo(staged);
            if (!copy.Exists || copy.Length != original.Length)
            {
                logger.Info($"Copying {original.Name} ({original.Length / (1024.0 * 1024 * 1024):0.0} GB) to {folder}...");
                File.Copy(modelPath, staged, overwrite: true);
            }

            return staged;
        }

        /// <summary>
        /// Deletes a staged copy; does nothing for an original.
        /// </summary>
        public static void Release(string loadedPath, string modelPath)
        {
            if (!string.Equals(loadedPath, modelPath, StringComparison.Ordinal) && File.Exists(loadedPath))
            {
                File.Delete(loadedPath);
            }
        }
    }
}
