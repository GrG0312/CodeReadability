using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Reporting
{
    /// <summary>
    /// Serialises a report object to disk as indented JSON.
    /// </summary>
    public static class JsonReportWriter
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // Reports are read by people and analysis tools, never embedded in HTML, so the
            // default escaping of ρ, Δ, χ² and "<" into \uXXXX only hurts readability.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Writes <paramref name="report"/> to <paramref name="outputPath"/>, creating the
        /// containing directory if it does not yet exist.
        /// </summary>
        ///
        /// <typeparam name="TReport">
        /// The file type, such as <see cref="Judgements.JudgementReport"/>.
        /// </typeparam>
        public static async Task WriteAsync<TReport>(
            TReport report,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(report, SerializerOptions);
            await File.WriteAllTextAsync(outputPath, json, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Converts <paramref name="value"/> into a JSON node with the same settings as the
        /// written files, so that it can be added to a file read as JSON.
        /// </summary>
        public static JsonNode? ToNode<T>(T value)
        {
            return JsonSerializer.SerializeToNode(value, SerializerOptions);
        }
    }
}
