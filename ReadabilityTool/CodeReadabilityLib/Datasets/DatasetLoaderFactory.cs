using CodeReadabilityLib.Datasets.Loaders;

namespace CodeReadabilityLib.Datasets
{
    public static class DatasetLoaderFactory
    {
        public static IDatasetLoader Create(string pathOrExtension)
        {
            string extension = Path.GetExtension(pathOrExtension);

            if (string.IsNullOrEmpty(extension))
            {
                extension = pathOrExtension;
            }

            return extension.ToLowerInvariant() switch
            {
                ".csv" or "csv" => new CsvDatasetLoader(),
                ".json" or "json" => new JsonDatasetLoader(),
                ".parquet" or "parquet" => new ParquetDatasetLoader(),
                _ => throw new ArgumentException($"Unsupported dataset type: {extension}")
            };
        }
    }
}
