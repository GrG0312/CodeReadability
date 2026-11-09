using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Checkpointing
{
    /// <summary>
    /// What an execution was asked to do, written before any work starts, so that
    /// <c>resume</c> can continue it with exactly the same settings.
    /// </summary>
    public sealed record ExecutionPlan
    {
        /// <summary>
        /// The command, such as <c>run-pipeline</c>.
        /// </summary>
        [JsonPropertyName("command")]
        public required string Command { get; init; }

        /// <summary>
        /// Every option as given on the command line.
        /// </summary>
        [JsonPropertyName("options")]
        public Dictionary<string, string> Options { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// The run folder the execution writes to.
        /// </summary>
        [JsonPropertyName("runFolder")]
        public required string RunFolder { get; init; }

        /// <summary>
        /// The ID-stamped dataset copy the execution reads.
        /// </summary>
        [JsonPropertyName("datasetPath")]
        public string? DatasetPath { get; init; }

        [JsonPropertyName("datasetId")]
        public string? DatasetId { get; init; }

        /// <summary>
        /// A hash of the dataset copy, to confirm on resume that it is unchanged.
        /// </summary>
        [JsonPropertyName("datasetSha256")]
        public string? DatasetSha256 { get; init; }

        /// <summary>
        /// The llama.cpp build the execution started with. A resume with a different build
        /// is refused, so that one execution never mixes versions.
        /// </summary>
        [JsonPropertyName("llamaCppVersion")]
        public string? LlamaCppVersion { get; init; }

        /// <summary>
        /// The output file of every step that has finished, keyed by step name.
        /// </summary>
        [JsonPropertyName("stepOutputs")]
        public Dictionary<string, string> StepOutputs { get; init; } = new Dictionary<string, string>();

        [JsonPropertyName("createdAtUtc")]
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
