using System.Text.Json;

namespace CodeReadabilityLib.Checkpointing
{
    /// <summary>
    /// Appends entries to a newline-delimited JSON file as they are produced.
    /// </summary>
    ///
    /// <remarks>
    /// Each line is flushed as it is written, so the file is always a valid partial record;
    /// at worst the last line is damaged by a crash mid-write, which the reader skips.
    /// </remarks>
    public sealed class JsonlCheckpointWriter : IAsyncDisposable
    {
        private readonly StreamWriter _writer;

        /// <summary>
        /// Serialises writes, because the pipelines score snippets concurrently and a
        /// <see cref="StreamWriter"/> is not thread-safe.
        /// </summary>
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Opens <paramref name="path"/> for appending, creating its directory if needed.
        /// </summary>
        public JsonlCheckpointWriter(string path)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool endsMidLine = EndsMidLine(path);
            _writer = new StreamWriter(path, append: true) { AutoFlush = true };

            // A crash can leave a damaged last line without a line break. Start a fresh line,
            // so that the first new entry is not glued onto it and lost with it.
            if (endsMidLine)
            {
                _writer.WriteLine();
            }
        }

        private static bool EndsMidLine(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            using FileStream stream = File.OpenRead(path);
            if (stream.Length == 0)
            {
                return false;
            }

            stream.Seek(-1, SeekOrigin.End);
            return stream.ReadByte() != '\n';
        }

        /// <summary>
        /// Appends <paramref name="entry"/> as a single JSON line.
        /// </summary>
        public async Task AppendAsync<T>(T entry, CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _writer.WriteLineAsync(JsonSerializer.Serialize(entry)).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await _writer.DisposeAsync().ConfigureAwait(false);
            _gate.Dispose();
        }
    }
}
