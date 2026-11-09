using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Reporting.Judgements
{
    /// <summary>
    /// Every answer of one run: one model queried with one prompting strategy over the
    /// whole dataset.
    /// </summary>
    public sealed record JudgedRun
    {
        /// <summary>
        /// The model name: the model file name without <c>.gguf</c>, with a
        /// <c>[thinking]</c> or <c>[no-thinking]</c> suffix for the two runs of a hybrid model.
        /// </summary>
        [JsonPropertyName("modelName")]
        public required string ModelName { get; init; }

        /// <summary>
        /// The id of the prompting strategy the model was queried with.
        /// </summary>
        [JsonPropertyName("promptingStrategy")]
        public required string PromptingStrategy { get; init; }

        /// <summary>
        /// Every snippet's answer, sorted by snippet id.
        /// </summary>
        [JsonPropertyName("concreteAnswers")]
        public List<ConcreteAnswer> ConcreteAnswers { get; init; } = new List<ConcreteAnswer>();

        /// <summary>
        /// A label identifying this model and strategy combination.
        /// </summary>
        [JsonIgnore]
        public string RunName => $"{ModelName} [{PromptingStrategy}]";
    }
}
