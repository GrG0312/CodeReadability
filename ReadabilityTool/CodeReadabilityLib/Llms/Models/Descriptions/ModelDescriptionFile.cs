using System.Text.Json;

namespace CodeReadabilityLib.Llms.Models.Descriptions
{
    /// <summary>
    /// Reads the user's model description file: a JSON object mapping each model name (the
    /// model file name without <c>.gguf</c>) to its <see cref="ModelDescription"/>.
    /// </summary>
    public static class ModelDescriptionFile
    {
        /// <summary>
        /// The file looked for in the model folder when no path is given.
        /// </summary>
        public const string DefaultFileName = "descriptions.json";

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// The description file to use: <paramref name="explicitPath"/> when given, else
        /// <see cref="DefaultFileName"/> in the model folder if it exists.
        /// </summary>
        ///
        /// <returns>
        /// <see langword="null"/> when no file was given and none exists in the model folder.
        /// </returns>
        ///
        /// <exception cref="FileNotFoundException">
        /// Thrown when an explicitly given file does not exist.
        /// </exception>
        public static string? Locate(string? explicitPath, string modelDirectory)
        {
            if (explicitPath is not null)
            {
                return File.Exists(explicitPath)
                    ? Path.GetFullPath(explicitPath)
                    : throw new FileNotFoundException($"Model description file not found: {explicitPath}", explicitPath);
            }

            string defaultPath = Path.Combine(modelDirectory, DefaultFileName);
            return File.Exists(defaultPath) ? defaultPath : null;
        }

        /// <summary>
        /// Loads a description file, keyed by model name ignoring case.
        /// </summary>
        ///
        /// <exception cref="InvalidDataException">
        /// Thrown when the file is not valid JSON, or a description has a <c>type</c> that is
        /// not allowed.
        /// </exception>
        public static Dictionary<string, ModelDescription> Load(string path)
        {
            Dictionary<string, ModelDescription>? descriptions;
            try
            {
                descriptions = JsonSerializer.Deserialize<Dictionary<string, ModelDescription>>(File.ReadAllText(path), Options);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"'{path}' is not a valid model description file: {ex.Message}", ex);
            }

            Dictionary<string, ModelDescription> result =
                new Dictionary<string, ModelDescription>(descriptions ?? new Dictionary<string, ModelDescription>(), StringComparer.OrdinalIgnoreCase);

            foreach ((string model, ModelDescription description) in result)
            {
                if (!ModelTypes.IsAllowed(description.Type))
                {
                    throw new InvalidDataException(
                        $"Model '{model}' in '{path}' has type '{description.Type}'. " +
                        $"Allowed: {ModelTypes.Reasoning}, {ModelTypes.Flash}, {ModelTypes.Hybrid}.");
                }
            }

            return result;
        }
    }
}
