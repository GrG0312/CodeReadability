using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Llms.Models.Descriptions
{
    /// <summary>
    /// What a model is, as described by hand: the characteristics the model-axes comparisons
    /// group models by.
    /// </summary>
    ///
    /// <remarks>
    /// Model file names follow no uniform scheme, so these are never derived from them.
    /// A value the user has not filled in yet holds <see cref="ModelTypes.Placeholder"/>.
    /// </remarks>
    public sealed record ModelDescription
    {
        [JsonPropertyName("family")]
        public string Family { get; init; } = ModelTypes.Placeholder;

        /// <summary>
        /// Free text, for example <c>14B</c>.
        /// </summary>
        [JsonPropertyName("size")]
        public string Size { get; init; } = ModelTypes.Placeholder;

        /// <summary>
        /// Free text, for example <c>Q4_K_M</c>.
        /// </summary>
        [JsonPropertyName("quant")]
        public string Quant { get; init; } = ModelTypes.Placeholder;

        /// <summary>
        /// One of the values on <see cref="ModelTypes"/>.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; init; } = ModelTypes.Placeholder;

        /// <summary>
        /// For a hybrid model whose template does not use the default switch.
        /// </summary>
        [JsonPropertyName("thinking-switch")]
        public ThinkingSwitch? ThinkingSwitch { get; init; }

        /// <summary>
        /// Requests specific GPUs instead of the automatic estimate, for example
        /// <c>vram24x2</c>.
        /// </summary>
        [JsonPropertyName("gpu")]
        public string? Gpu { get; init; }

        /// <summary>
        /// The model file the results came from, without <c>.gguf</c>. Written by the tool;
        /// both runs of a hybrid model point at the same file.
        /// </summary>
        [JsonPropertyName("model-file")]
        public string? ModelFile { get; init; }

        /// <summary>
        /// Whether any characteristic still holds the placeholder.
        /// </summary>
        [JsonIgnore]
        public bool HasPlaceholders =>
            new[] { Family, Size, Quant, Type }.Any(value => ModelTypes.Normalize(value) == ModelTypes.Placeholder);

        /// <summary>
        /// Whether the description marks a hybrid model.
        /// </summary>
        [JsonIgnore]
        public bool IsHybrid => ModelTypes.Normalize(Type) == ModelTypes.Hybrid;
    }
}
