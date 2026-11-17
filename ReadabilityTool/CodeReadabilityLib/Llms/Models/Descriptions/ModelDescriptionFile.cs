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
        /// Thrown when the file is not valid JSON, a description has a <c>type</c> that is not
        /// allowed, or its recommended parameters are missing for a mode or not valid.
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

                CheckRecommendedParams(model, description, path);
            }

            return result;
        }

        /// <summary>
        /// Requires a block for every mode the model runs in, so that a forgotten block
        /// cannot silently run one model with different settings from the others, and checks
        /// that every value is a number under an allowed name.
        /// </summary>
        ///
        /// <exception cref="InvalidDataException"></exception>
        private static void CheckRecommendedParams(string model, ModelDescription description, string path)
        {
            foreach (bool thinking in description.ThinkingModes)
            {
                string mode = thinking ? "thinking" : "no-thinking";
                Dictionary<string, JsonElement> block = description.RecommendedParams?.For(thinking)
                    ?? throw new InvalidDataException(
                        $"Model '{model}' in '{path}' has no 'recommended-params' block for '{mode}'. " +
                        "Write the recommended values, or an empty block {} to keep the server's defaults on purpose.");

                foreach ((string name, JsonElement value) in block)
                {
                    if (!SamplingParameterNames.Allowed.Contains(name))
                    {
                        throw new InvalidDataException(
                            $"Model '{model}' in '{path}' has the unknown parameter '{name}' under '{mode}'. " +
                            $"Allowed: {string.Join(", ", SamplingParameterNames.Allowed)}.");
                    }

                    if (value.ValueKind != JsonValueKind.Number)
                    {
                        throw new InvalidDataException($"Model '{model}' in '{path}' has a non-numeric '{name}' under '{mode}'.");
                    }
                }
            }
        }
    }
}
