using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Storage.Tracking;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Reporting.Judgements
{
    /// <summary>
    /// The file written by <c>mass-judge</c>: a header, the model descriptions, and every
    /// model answer. It holds no statistics; those are added by <c>compute-statistics</c>.
    /// </summary>
    public sealed record JudgementReport
    {
        [JsonPropertyName("generatedAtUtc")]
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// The dataset ID and input files this file was made from.
        /// </summary>
        [JsonPropertyName("provenance")]
        public required Provenance Provenance { get; init; }

        /// <summary>
        /// The llama.cpp build the models ran on, as reported by the model server.
        /// </summary>
        [JsonPropertyName("llamaCppVersion")]
        public string? LlamaCppVersion { get; set; }

        /// <summary>
        /// Human-readable description of the dataset the models judged.
        /// </summary>
        [JsonPropertyName("datasetDescription")]
        public required string DatasetDescription { get; init; }

        /// <summary>
        /// The number of snippets every run was asked to score.
        /// </summary>
        [JsonPropertyName("totalSnippets")]
        public required int TotalSnippets { get; init; }

        /// <summary>
        /// One description per model name that appears in <see cref="Runs"/>. Values the
        /// user has not filled in yet hold <see cref="ModelTypes.Placeholder"/>.
        /// </summary>
        [JsonPropertyName("model-descriptions")]
        public Dictionary<string, ModelDescription> ModelDescriptions { get; init; } = new Dictionary<string, ModelDescription>();

        /// <summary>
        /// One entry per model and prompting strategy.
        /// </summary>
        [JsonPropertyName("runs")]
        public List<JudgedRun> Runs { get; init; } = new List<JudgedRun>();
    }
}
