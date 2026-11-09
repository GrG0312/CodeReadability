using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Llms.Models.Descriptions
{
    /// <summary>
    /// The chat template values that switch a hybrid model's thinking on and off.
    /// </summary>
    ///
    /// <remarks>
    /// There is no universal switch: each model family defines its own in its chat template.
    /// The default fits the Qwen hybrids; other models describe their own.
    /// </remarks>
    public sealed record ThinkingSwitch
    {
        /// <summary>
        /// Template values that switch thinking on.
        /// </summary>
        [JsonPropertyName("on")]
        public Dictionary<string, JsonElement> On { get; init; } = new Dictionary<string, JsonElement>();

        /// <summary>
        /// Template values that switch thinking off.
        /// </summary>
        [JsonPropertyName("off")]
        public Dictionary<string, JsonElement> Off { get; init; } = new Dictionary<string, JsonElement>();

        /// <summary>
        /// The <c>enable_thinking: true / false</c> template flag.
        /// </summary>
        public static ThinkingSwitch Default { get; } = new ThinkingSwitch
        {
            On = new Dictionary<string, JsonElement> { ["enable_thinking"] = JsonSerializer.SerializeToElement(true) },
            Off = new Dictionary<string, JsonElement> { ["enable_thinking"] = JsonSerializer.SerializeToElement(false) }
        };
    }
}
