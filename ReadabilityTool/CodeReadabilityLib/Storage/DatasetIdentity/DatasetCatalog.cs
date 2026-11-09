using CodeReadabilityLib.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CodeReadabilityLib.Storage.DatasetIdentity
{
    /// <summary>
    /// Gives every dataset a stable identity by keeping ID-stamped copies of it.
    /// </summary>
    ///
    /// <remarks>
    /// The ID lives in the file name, <c>&lt;name&gt;__visi-&lt;yyyyMMdd-HHmmss&gt;.&lt;ext&gt;</c>,
    /// so that output files made from the same dataset can be matched by comparing one
    /// string. The user's own file is never modified or renamed: a file without an ID is
    /// copied, and the copy is used.
    /// </remarks>
    public sealed partial class DatasetCatalog
    {
        /// <summary>
        /// Separates the dataset name from its ID.
        /// </summary>
        public const string IdMarker = "__visi-";

        private const string IdFormat = "yyyyMMdd-HHmmss";

        [GeneratedRegex(@"^(?<name>.+)__visi-(?<id>\d{8}-\d{6})$")]
        private static partial Regex IdPattern();

        private readonly string _directory;
        private readonly ILogger _logger;

        /// <param name="directory">
        /// Where the copies are kept, normally <see cref="Layout.OutputLayout.DatasetsDirectory"/>.
        /// </param>
        public DatasetCatalog(string directory, ILogger logger)
        {
            _directory = directory;
            _logger = logger;
        }

        /// <summary>
        /// Splits a file name into the dataset name and its ID.
        /// </summary>
        ///
        /// <returns>
        /// <see langword="false"/> when the name carries no ID.
        /// </returns>
        public static bool TryParseId(string path, out string name, out string id)
        {
            Match match = IdPattern().Match(Path.GetFileNameWithoutExtension(path));
            name = match.Success ? match.Groups["name"].Value : Path.GetFileNameWithoutExtension(path);
            id = match.Success ? match.Groups["id"].Value : string.Empty;
            return match.Success;
        }

        /// <summary>
        /// Returns the ID-stamped copy of <paramref name="path"/> to use, creating one when
        /// none exists or the latest one differs from the file.
        /// </summary>
        ///
        /// <exception cref="FileNotFoundException">
        /// Thrown when <paramref name="path"/> does not exist.
        /// </exception>
        public ResolvedDataset Resolve(string path, DateTime now)
        {
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Dataset not found: {path}", fullPath);
            }

            if (TryParseId(fullPath, out string name, out string id))
            {
                return new ResolvedDataset { Path = fullPath, Id = id, Name = name, GivenPath = fullPath };
            }

            string extension = Path.GetExtension(fullPath);
            Directory.CreateDirectory(_directory);

            // The ID is a sortable timestamp, so the latest copy sorts last.
            string? latest = Directory.GetFiles(_directory, $"{name}{IdMarker}*{extension}")
                .Where(file => TryParseId(file, out string copyName, out _) && copyName == name)
                .OrderBy(file => file, StringComparer.Ordinal)
                .LastOrDefault();

            if (latest is not null && SameContent(fullPath, latest))
            {
                TryParseId(latest, out _, out string latestId);
                _logger.Info($"Using dataset copy {Path.GetFileName(latest)} (identical to {Path.GetFileName(fullPath)}).");
                return new ResolvedDataset { Path = latest, Id = latestId, Name = name, GivenPath = fullPath };
            }

            DateTime stamp = now;
            string copy;
            do
            {
                copy = Path.Combine(_directory, $"{name}{IdMarker}{stamp.ToString(IdFormat, CultureInfo.InvariantCulture)}{extension}");
                stamp = stamp.AddSeconds(1);
            }
            while (File.Exists(copy));

            File.Copy(fullPath, copy);
            TryParseId(copy, out _, out string newId);

            _logger.Info(latest is null
                ? $"Dataset {Path.GetFileName(fullPath)} had no copy yet; created {Path.GetFileName(copy)}."
                : $"Dataset {Path.GetFileName(fullPath)} changed since its last copy; created {Path.GetFileName(copy)}.");

            return new ResolvedDataset { Path = copy, Id = newId, Name = name, GivenPath = fullPath };
        }

        private static bool SameContent(string first, string second)
        {
            FileInfo a = new FileInfo(first);
            FileInfo b = new FileInfo(second);
            if (a.Length != b.Length)
            {
                return false;
            }

            using FileStream left = a.OpenRead();
            using FileStream right = b.OpenRead();

            byte[] leftBuffer = new byte[81920];
            byte[] rightBuffer = new byte[81920];
            int read;
            while ((read = left.Read(leftBuffer, 0, leftBuffer.Length)) > 0)
            {
                right.ReadExactly(rightBuffer, 0, read);
                if (!leftBuffer.AsSpan(0, read).SequenceEqual(rightBuffer.AsSpan(0, read)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
