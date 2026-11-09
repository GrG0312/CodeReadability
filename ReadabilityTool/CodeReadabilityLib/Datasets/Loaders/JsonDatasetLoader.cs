using CodeReadabilityLib.Sources;
using System.Text.Json;

namespace CodeReadabilityLib.Datasets.Loaders
{
    public sealed class JsonDatasetLoader : IDatasetLoader
    {
        private sealed class JsonRow
        {
            public required string Id { get; set; }
            public required string Code { get; set; }
            public int Score { get; set; }
        }

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public List<CodeSnippet> Load(string path)
        {
            string json = File.ReadAllText(path);
            JsonRow[]? rows = JsonSerializer.Deserialize<JsonRow[]>(json, _jsonSerializerOptions);

            if (rows is null || rows.Length == 0)
            {
                return new List<CodeSnippet>();
            }

            return rows
                .Where(row => !string.IsNullOrWhiteSpace(row.Code))
                .Select((JsonRow row, int index) => new CodeSnippet(
                    id: string.IsNullOrWhiteSpace(row.Id) ? $"row-{index + 1}" : row.Id,
                    code: row.Code,
                    score: row.Score)
                )
                .ToList();
        }
    }
}
