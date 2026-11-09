namespace CodeReadabilityLib.Storage.DatasetIdentity
{
    /// <summary>
    /// A dataset as the steps use it: an ID-stamped copy kept by the tool.
    /// </summary>
    public sealed record ResolvedDataset
    {
        /// <summary>
        /// The file the steps read: the tool's copy, or the given file when it already
        /// carried an ID.
        /// </summary>
        public required string Path { get; init; }

        /// <summary>
        /// The dataset ID, a <c>yyyyMMdd-HHmmss</c> stamp.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// The dataset name without its ID, used to name output files.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// The file the user gave.
        /// </summary>
        public required string GivenPath { get; init; }
    }
}
