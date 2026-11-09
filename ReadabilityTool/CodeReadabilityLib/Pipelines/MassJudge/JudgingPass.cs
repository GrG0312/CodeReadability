using CodeReadabilityLib.Llms.Models.Descriptions;
using System.Text.Json;

namespace CodeReadabilityLib.Pipelines.MassJudge
{
    /// <summary>
    /// One way of running a loaded model: the whole model, or one mode of a hybrid model.
    /// </summary>
    ///
    /// <remarks>
    /// A hybrid model file yields two passes on the same loaded server, one with thinking on
    /// and one with it off. Every later step treats them as two models.
    /// </remarks>
    public sealed record JudgingPass
    {
        /// <summary>
        /// The model name the pass's results appear under.
        /// </summary>
        public required string ModelName { get; init; }

        /// <summary>
        /// The pass's description as written to the judgement file.
        /// </summary>
        public required ModelDescription Description { get; init; }

        /// <summary>
        /// Chat template values sent with every request, or <see langword="null"/>.
        /// </summary>
        public IReadOnlyDictionary<string, JsonElement>? ChatTemplateValues { get; init; }

        /// <summary>
        /// Extra completion tokens for thinking.
        /// </summary>
        public required uint ThinkingTokenBudget { get; init; }

        /// <summary>
        /// The passes for one model file.
        /// </summary>
        ///
        /// <param name="modelFile">
        /// The model file name without <c>.gguf</c>.
        /// </param>
        ///
        /// <param name="description">
        /// The user's description of the model, or placeholders when it has none.
        /// </param>
        public static IReadOnlyList<JudgingPass> For(string modelFile, ModelDescription description, uint thinkingTokenBudget)
        {
            ModelDescription fileDescription = description with { ModelFile = modelFile };

            if (!description.IsHybrid)
            {
                bool flash = ModelTypes.Normalize(description.Type) == ModelTypes.Flash;
                return
                [
                    new JudgingPass
                    {
                        ModelName = modelFile,
                        Description = fileDescription,
                        ThinkingTokenBudget = flash ? 0 : thinkingTokenBudget
                    }
                ];
            }

            ThinkingSwitch thinkingSwitch = description.ThinkingSwitch ?? ThinkingSwitch.Default;

            return
            [
                new JudgingPass
                {
                    ModelName = $"{modelFile} [thinking]",
                    Description = fileDescription with { Type = ModelTypes.Reasoning, ThinkingSwitch = null },
                    ChatTemplateValues = thinkingSwitch.On,
                    ThinkingTokenBudget = thinkingTokenBudget
                },
                new JudgingPass
                {
                    ModelName = $"{modelFile} [no-thinking]",
                    Description = fileDescription with { Type = ModelTypes.Flash, ThinkingSwitch = null },
                    ChatTemplateValues = thinkingSwitch.Off,
                    ThinkingTokenBudget = 0
                }
            ];
        }
    }
}
