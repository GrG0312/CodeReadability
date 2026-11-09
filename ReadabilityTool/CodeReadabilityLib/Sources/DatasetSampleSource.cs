using CodeReadabilityLib.Datasets;
using CodeReadabilityLib.Languages;
using System.Runtime.CompilerServices;

namespace CodeReadabilityLib.Sources
{
    /// <summary>
    /// Yields code samples from a labelled dataset file on disk.
    /// </summary>
    ///
    /// <remarks>
    /// The loaders deliberately know nothing about programming languages, so the language is
    /// attached here rather than being inferred per row. Every sample from one dataset is
    /// therefore assumed to be written in the same language.
    /// </remarks>
    public sealed class DatasetSampleSource : ICodeSampleSource
    {
        private readonly string _path;
        private readonly SupportedLanguage _language;
        private readonly IDatasetLoader _datasetLoader;

        /// <inheritdoc/>
        public string Description => $"dataset '{Path.GetFileName(_path)}'";

        public DatasetSampleSource(string path, SupportedLanguage language, IDatasetLoader datasetLoader)
        {
            _path = path;
            _language = language;
            _datasetLoader = datasetLoader;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<CodeSample> GetSamplesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // The loaders are synchronous and file-bound, so the read is pushed onto the
            // thread pool to keep this method from blocking its caller.
            List<CodeSnippet> snippets =
                await Task.Run(() => _datasetLoader.Load(_path), cancellationToken).ConfigureAwait(false);

            foreach (CodeSnippet snippet in snippets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new CodeSample
                {
                    Id = snippet.Id,
                    Code = snippet.Code,
                    Language = _language,
                    ReferenceScore = snippet.Score
                };
            }
        }
    }
}
