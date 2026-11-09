using System.Text.Json;

namespace CodeReadabilityLib.Llms.Chatting
{
    /// <summary>
    /// Represents the options for configuring a chat session with a language model.
    /// </summary>
    public sealed record ChatOptions
    {
        /// <summary>
        /// The temperature parameter controls the randomness of the model's output. 
        /// A higher temperature (e.g., 1.0) will result in more diverse and creative responses,
        /// while a lower temperature (e.g., 0.0) will make the output more deterministic and focused.
        /// </summary>
        public double Temperature { get; init; } = 0.0;

        /// <summary>
        /// The max tokens parameter specifies the maximum number of tokens (words or subwords) that the model can generate in its response.
        /// If zero, the model will generate as many tokens as it can until it reaches the end of its response or hits a limit set by the API.
        /// </summary>
        public uint MaxTokens { get; init; } = 512;

        /// <summary>
        /// The seed parameter is an optional integer value that can be used to initialize the random number generator for the model's output.
        /// </summary>
        public int? Seed { get; init; } = 35420;

        /// <summary>
        /// The jsonSchema parameter is an optional string that can be used to specify a JSON schema for the model's output.
        /// llama.cpp converts this to a GBNF grammar, which makes
        /// malformed output structurally impossible rather than something we parse around.
        /// </summary>
        public string? JsonSchema { get; init; }

        /// <summary>
        /// Extra values passed to the model's chat template with the request, such as
        /// <c>enable_thinking: false</c> to switch a hybrid model's thinking off.
        /// <see langword="null"/> sends none.
        /// </summary>
        public IReadOnlyDictionary<string, JsonElement>? ChatTemplateValues { get; init; }

        public static readonly ChatOptions Deterministic = new ChatOptions();
    }
}
