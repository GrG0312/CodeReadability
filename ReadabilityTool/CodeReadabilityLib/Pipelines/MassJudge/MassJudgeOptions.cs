using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Llms.Judging.Prompting;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Llms.ServerHosting;

namespace CodeReadabilityLib.Pipelines.MassJudge
{
    /// <summary>
    /// Configures a <see cref="MassJudgePipeline"/> run.
    /// </summary>
    public sealed record MassJudgeOptions
    {
        /// <summary>
        /// Directory searched (non-recursively) for the <c>.gguf</c> models to run.
        /// </summary>
        public required string ModelDirectory { get; init; }

        /// <summary>
        /// Server settings shared by every model. The pipeline overrides
        /// <see cref="LlamaServerOptions.ModelPath"/> and
        /// <see cref="LlamaServerOptions.ParallelSlots"/> for each model in turn.
        /// </summary>
        public required LlamaServerOptions ServerTemplate { get; init; }

        /// <summary>
        /// The user's model descriptions, keyed by model name. A model missing from it gets
        /// placeholder values.
        /// </summary>
        public IReadOnlyDictionary<string, ModelDescription> Descriptions { get; init; } =
            new Dictionary<string, ModelDescription>();

        /// <summary>
        /// When set, only this model file (without <c>.gguf</c>) is judged; used by the
        /// per-model jobs of a <c>multi-job</c> execution.
        /// </summary>
        public string? OnlyModel { get; init; }

        /// <summary>
        /// The prompting strategies every model is queried with.
        /// </summary>
        public IReadOnlyList<IPromptingStrategy> Strategies { get; init; } = PromptingStrategies.All;

        /// <summary>
        /// How many snippets to score at once.
        /// </summary>
        ///
        /// <remarks>
        /// This also becomes the server's parallel slot count, so raising it increases GPU
        /// memory use. Leave it at 1 when memory is tight.
        /// </remarks>
        public int Concurrency { get; init; } = 1;

        /// <summary>
        /// Where every answer is saved as it arrives, and where finished work is found again
        /// when the execution is resumed.
        /// </summary>
        public required CheckpointStore Checkpoints { get; init; }

        /// <summary>
        /// Extra completion tokens for a model's thinking before the answer. Not granted to
        /// runs known not to think (flash models, and hybrid models with thinking off).
        /// </summary>
        ///
        /// <remarks>
        /// Too small a budget cuts the thinking off before any score is produced, which shows
        /// up as unusable answers. The prompt, the code and this budget must all fit in the
        /// server's context size.
        /// </remarks>
        public uint ThinkingTokenBudget { get; init; } = 2048;
    }
}
