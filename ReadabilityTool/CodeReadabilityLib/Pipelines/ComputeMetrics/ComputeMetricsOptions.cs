using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Languages;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Llms.ServerHosting;
using CodeReadabilityLib.Reporting.MetricResults;

namespace CodeReadabilityLib.Pipelines.ComputeMetrics
{
    /// <summary>
    /// Configures a <see cref="ComputeMetricsPipeline"/> run.
    /// </summary>
    public sealed record ComputeMetricsOptions
    {
        /// <summary>
        /// The language every snippet is written in; the algorithmic metrics depend on it.
        /// </summary>
        public required SupportedLanguage Language { get; init; }

        /// <summary>
        /// One of the values on <see cref="MetricKinds"/>.
        /// </summary>
        public string Kinds { get; init; } = MetricKinds.Both;

        /// <summary>
        /// Directory of the <c>.gguf</c> models that grade the LLM-based metrics. Required
        /// unless <see cref="Kinds"/> is <see cref="MetricKinds.AlgorithmicOnly"/>.
        /// </summary>
        public string? ModelDirectory { get; init; }

        /// <summary>
        /// When set, only this model file (without <c>.gguf</c>) grades; used by the
        /// per-model jobs of a <c>multi-job</c> execution.
        /// </summary>
        public string? OnlyModel { get; init; }

        /// <summary>
        /// Server settings shared by every model; the model path is set per model.
        /// </summary>
        public LlamaServerOptions? ServerTemplate { get; init; }

        /// <summary>
        /// The user's model descriptions, used to switch a hybrid model's thinking off.
        /// </summary>
        public IReadOnlyDictionary<string, ModelDescription> Descriptions { get; init; } =
            new Dictionary<string, ModelDescription>();

        /// <summary>
        /// How many snippets to grade at once; also the server's parallel slot count.
        /// </summary>
        public int Concurrency { get; init; } = 1;

        /// <summary>
        /// Where every grading is saved as it arrives, and where finished work is found again
        /// on resume. Required for LLM-based metrics.
        /// </summary>
        public CheckpointStore? Checkpoints { get; init; }

        /// <summary>
        /// Extra completion tokens for thinking. Not granted to flash models, nor to hybrid
        /// models, which grade with thinking off.
        /// </summary>
        public uint ThinkingTokenBudget { get; init; } = 2048;
    }
}
