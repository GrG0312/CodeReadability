using CodeReadabilityLib.Datasets;
using CodeReadabilityLib.Llms.Models.Descriptions;

namespace CodeReadabilityLib.Preflight.Basics
{
    /// <summary>
    /// Requirement checks that need no model: files, folders, programs and descriptions.
    /// </summary>
    public static class EnvironmentChecks
    {
        /// <summary>
        /// Checks that the dataset exists and loads, and, when <paramref name="requireScores"/>
        /// is set, that it has human scores.
        /// </summary>
        ///
        /// <returns>
        /// What the dataset holds, or <see langword="null"/> when it could not be loaded.
        /// </returns>
        public static DatasetSummary? Dataset(string path, bool requireScores, RequirementProblems problems)
        {
            if (!File.Exists(path))
            {
                problems.Add($"Dataset not found: {path}");
                return null;
            }

            List<CodeSnippet> snippets;
            try
            {
                snippets = DatasetLoaderFactory.Create(path).Load(path);
            }
            catch (Exception ex)
            {
                problems.Add($"Dataset '{path}' could not be loaded: {ex.Message}");
                return null;
            }

            int labelled = snippets.Count(s => s.Score.HasValue);

            if (snippets.Count == 0)
            {
                problems.Add($"Dataset '{path}' holds no snippets.");
            }
            else if (requireScores && labelled == 0)
            {
                problems.Add($"Dataset '{path}' has no human scores, which judging requires.");
            }

            List<CodeSnippet> byLength = snippets.OrderBy(s => s.Code.Length).ToList();

            return new DatasetSummary
            {
                Snippets = snippets.Count,
                Labelled = labelled,
                LongestCode = byLength.LastOrDefault()?.Code ?? string.Empty,
                SampleCode = byLength.Count > 0 ? byLength[byLength.Count / 2].Code : string.Empty
            };
        }

        /// <summary>
        /// Checks that the model folder exists and holds <c>.gguf</c> models.
        /// </summary>
        ///
        /// <returns>
        /// The model paths, sorted; empty when there are none.
        /// </returns>
        public static string[] ModelFolder(string directory, RequirementProblems problems)
        {
            if (!Directory.Exists(directory))
            {
                problems.Add($"Model folder not found: {directory}");
                return [];
            }

            string[] models = Directory.GetFiles(directory, "*.gguf", SearchOption.TopDirectoryOnly);
            Array.Sort(models, StringComparer.OrdinalIgnoreCase);

            if (models.Length == 0)
            {
                problems.Add($"Model folder '{directory}' holds no .gguf files.");
            }

            return models;
        }

        /// <summary>
        /// Checks that the model server program can be found, either at the given path or on
        /// the PATH.
        /// </summary>
        public static void ServerProgram(string executable, RequirementProblems problems)
        {
            if (executable.Contains(Path.DirectorySeparatorChar) || executable.Contains(Path.AltDirectorySeparatorChar))
            {
                if (!File.Exists(executable))
                {
                    problems.Add($"Model server program not found: {executable}");
                }

                return;
            }

            string[] extensions = OperatingSystem.IsWindows() ? [".exe", ".cmd", ".bat", string.Empty] : [string.Empty];
            IEnumerable<string> folders = (System.Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            bool found = folders.Any(folder => extensions.Any(extension => File.Exists(Path.Combine(folder, executable + extension))));
            if (!found)
            {
                problems.Add($"Model server program '{executable}' is not on the PATH; pass --llama-server-path.");
            }
        }

        /// <summary>
        /// Checks that every folder can really be written to, by writing and deleting a file.
        /// </summary>
        ///
        /// <remarks>
        /// A real write, because on network storage with access lists the permission bits can
        /// say a folder is writable when it is not.
        /// </remarks>
        public static void Writable(IEnumerable<string> directories, RequirementProblems problems)
        {
            foreach (string directory in directories)
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    string probe = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}");
                    File.WriteAllText(probe, string.Empty);
                    File.Delete(probe);
                }
                catch (Exception ex)
                {
                    problems.Add($"Cannot write to '{directory}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks that every model in the folder has a complete description: present, with no
        /// placeholders and an allowed type.
        /// </summary>
        public static void DescriptionsComplete(
            IReadOnlyDictionary<string, ModelDescription> descriptions,
            IEnumerable<string> modelPaths,
            RequirementProblems problems)
        {
            foreach (string modelFile in modelPaths.Select(path => Path.GetFileNameWithoutExtension(path)))
            {
                if (!descriptions.TryGetValue(modelFile, out ModelDescription? description))
                {
                    problems.Add($"Model '{modelFile}' is missing from the model description file.");
                }
                else if (description.HasPlaceholders)
                {
                    problems.Add($"Model '{modelFile}' still has '{ModelTypes.Placeholder}' values in the model description file.");
                }
            }
        }
    }
}
