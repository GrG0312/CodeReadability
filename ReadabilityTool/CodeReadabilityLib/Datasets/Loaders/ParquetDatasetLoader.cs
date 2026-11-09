using ParquetSharp;

namespace CodeReadabilityLib.Datasets.Loaders
{
    public sealed class ParquetDatasetLoader : IDatasetLoader
    {
        public List<CodeSnippet> Load(string path)
        {
            List<CodeSnippet> samples = new List<CodeSnippet>();
            using ParquetFileReader reader = new ParquetFileReader(path);

            for (int rowGroup = 0; rowGroup < reader.FileMetaData.NumRowGroups; rowGroup++)
            {
                using RowGroupReader rowGroupReader = reader.RowGroup(rowGroup);

                int numberOfRowsInGroup = checked((int)rowGroupReader.MetaData.NumRows);

                string[] snippets = rowGroupReader.Column(0).LogicalReader<string>().ReadAll(numberOfRowsInGroup);
                double?[] scores = rowGroupReader.Column(1).LogicalReader<double?>().ReadAll(numberOfRowsInGroup);

                if (snippets.Length != scores.Length)
                {
                    throw new InvalidDataException($"Mismatch in number of snippets and scores in row group {rowGroup}");
                }

                for (int i = 0; i < snippets.Length; i++)
                {
                    samples.Add(new CodeSnippet(
                        id: $"{Path.GetFileNameWithoutExtension(path)}-rg{rowGroup}-{i}",
                        code: snippets[i],
                        score: scores[i]
                    ));
                }
            }

            return samples;
        }
    }
}
