using Microsoft.VisualBasic.FileIO;
using System.Globalization;

namespace CodeReadabilityLib.Datasets.Loaders
{
    /// <summary>
    /// Loads labelled code snippets from a comma-separated file.
    /// </summary>
    ///
    /// <remarks>
    /// The file must have a header row containing at least <c>code</c> and <c>score</c>
    /// columns. An <c>id</c> column is used when present, otherwise a positional
    /// <c>row-N</c> identifier is generated so that runs stay reproducible.
    /// </remarks>
    public sealed class CsvDatasetLoader : IDatasetLoader
    {
        public List<CodeSnippet> Load(string path)
        {
            List<CodeSnippet> samples = new List<CodeSnippet>();

            using TextFieldParser parser = new TextFieldParser(path);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;

            string[]? header = parser.ReadFields();
            if (header is null)
            {
                return new List<CodeSnippet>();
            }

            int idIndex = Find(header, "id");
            int codeIndex = Find(header, "code");
            int scoreIndex = Find(header, "score");

            if (codeIndex < 0 || scoreIndex < 0)
            {
                throw new InvalidOperationException("CSV must contain at least 'code' and 'score' columns.");
            }

            int row = 0;
            while (!parser.EndOfData)
            {
                row++;
                string[]? fields = parser.ReadFields();
                if (fields is null) continue;

                string code = Get(fields, codeIndex);
                if (string.IsNullOrWhiteSpace(code)) continue;

                string id = idIndex >= 0 ? Get(fields, idIndex) : $"row-{row}";

                samples.Add(new CodeSnippet(
                    id: string.IsNullOrWhiteSpace(id) ? $"row-{row}" : id,
                    code: code,
                    score: ParseScore(Get(fields, scoreIndex))));
            }

            return samples;
        }

        private static int Find(string[] header, string name)
        {
            return Array.FindIndex(header, h => string.Equals(h.Trim(), name, StringComparison.OrdinalIgnoreCase));
        }

        private static string Get(string[] fields, int index)
        {
            if (index >= 0 && index < fields.Length)
            {
                return fields[index];
            }
            return string.Empty;
        }

        private static double? ParseScore(string raw)
        {
            // Do not clamp here.
            // A row whose score is out of range is a dataset problem,
            // and silently pulling it to 1 or 5 fabricates ground truth.
            //
            // Invariant culture is mandatory: under a locale that uses ',' as the decimal
            // separator (such as hu-HU) a plain TryParse rejects "3.5" and the score is
            // silently lost, which would quietly drop rows from the dataset.
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : null;
        }
    }
}
