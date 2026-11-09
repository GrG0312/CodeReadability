using CodeReadabilityLib.Storage.DatasetIdentity;
using CodeReadabilityLib.Storage.Layout;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Storage.Tracking
{
    /// <summary>
    /// Where an output file came from, recorded in every output file so that it can be
    /// traced back to its inputs.
    /// </summary>
    public sealed record Provenance
    {
        /// <summary>
        /// The ID of the dataset the file was made from. Files made from the same dataset
        /// carry the same ID.
        /// </summary>
        [JsonPropertyName("datasetId")]
        public required string DatasetId { get; init; }

        /// <summary>
        /// The dataset name without its ID.
        /// </summary>
        [JsonPropertyName("datasetName")]
        public required string DatasetName { get; init; }

        /// <summary>
        /// The input files the output was made from, keyed by role, such as
        /// <c>dataset</c> or <c>modelDirectory</c>.
        /// </summary>
        [JsonPropertyName("inputs")]
        public Dictionary<string, string> Inputs { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// The run folder the output was written to.
        /// </summary>
        [JsonPropertyName("runFolder")]
        public string? RunFolder { get; init; }

        /// <summary>
        /// The tool version that wrote the output.
        /// </summary>
        [JsonPropertyName("toolVersion")]
        public string ToolVersion { get; init; } = LibraryInfo.Version;

        /// <summary>
        /// Builds the provenance of an output made directly from a dataset.
        /// </summary>
        public static Provenance FromDataset(ResolvedDataset dataset, RunFolder? run, IReadOnlyDictionary<string, string>? otherInputs = null)
        {
            Dictionary<string, string> inputs = new Dictionary<string, string> { ["dataset"] = dataset.Path };
            if (otherInputs is not null)
            {
                foreach ((string role, string path) in otherInputs)
                {
                    inputs[role] = path;
                }
            }

            return new Provenance
            {
                DatasetId = dataset.Id,
                DatasetName = dataset.Name,
                Inputs = inputs,
                RunFolder = run?.Path
            };
        }
    }
}
