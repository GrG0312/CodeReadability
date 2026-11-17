using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Llms.Models.Descriptions
{
    /// <summary>
    /// The sampling parameters a model's makers recommend, one block per mode, sent with
    /// every request of that mode.
    /// </summary>
    ///
    /// <remarks>
    /// The keys are llama.cpp request fields (see <see cref="SamplingParameterNames"/>). A key
    /// left out takes the server's default: the model file's own recommendation if it carries
    /// one, else llama.cpp's. An empty block leaves every value at the default on purpose; a
    /// missing block is an error for a mode the model runs in.
    /// </remarks>
    public sealed record RecommendedParams
    {
        /// <summary>
        /// The parameters for runs with thinking on.
        /// </summary>
        [JsonPropertyName("thinking")]
        public Dictionary<string, JsonElement>? Thinking { get; init; }

        /// <summary>
        /// The parameters for runs with thinking off.
        /// </summary>
        [JsonPropertyName("no-thinking")]
        public Dictionary<string, JsonElement>? NoThinking { get; init; }

        /// <summary>
        /// The block for one mode, or <see langword="null"/> when it is missing.
        /// </summary>
        public Dictionary<string, JsonElement>? For(bool thinking)
        {
            return thinking ? Thinking : NoThinking;
        }
    }
}
