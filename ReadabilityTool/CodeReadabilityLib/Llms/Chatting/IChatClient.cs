namespace CodeReadabilityLib.Llms.Chatting
{
    /// <summary>
    /// Represents a client for interacting with a chat-based language model.
    /// </summary>
    public interface IChatClient : IAsyncDisposable
    {
        /// <summary>
        /// Asynchronously generates a response from the chat model based on the provided system and user prompts, along with specified options.
        /// </summary>
        /// 
        /// <param name="systemPrompt">
        /// The system prompt that provides context or instructions to the chat model.
        /// </param>
        /// 
        /// <param name="userPrompt">
        /// The user prompt that represents the input or query from the user to which the chat model should respond.
        /// </param>
        /// 
        /// <param name="options">
        /// The options that configure the behavior of the chat model, such as temperature, maximum tokens, and other parameters.
        /// </param>
        /// 
        /// <param name="cancellationToken">
        /// A token that can be used to cancel the asynchronous operation if needed.
        /// </param>
        /// 
        /// <returns>
        /// A task that represents the asynchronous operation. 
        /// The task result contains the reply, the separately returned thinking, and why generation stopped.
        /// </returns>
        public Task<ChatCompletion> CompleteAsync(string systemPrompt, string userPrompt, ChatOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the maximum number of concurrent requests that the chat client can handle.
        /// </summary>
        public uint MaxConcurrentRequests { get; }
    }
}
