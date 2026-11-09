using CodeReadabilityLib.Llms.Chatting;
using CodeReadabilityLib.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace CodeReadabilityLib.Llms.ServerHosting
{
    /// <summary>
    /// Represents a client for interacting with a LlamaCpp server.
    /// This class provides methods to start a new server, attach to an existing server, and perform chat completions using the server's API.
    /// </summary>
    public sealed class LlamaCppClient : IChatClient
    {
        /// <summary>
        /// The <see cref="HttpClient"/> used to send requests to the LlamaCpp server.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// The <see cref="LlamaServerHost"/> instance that this client owns, if any.
        /// </summary>
        private readonly LlamaServerHost? _ownedHost;


        /// <summary>
        /// The name of the model that the server is using.
        /// </summary>
        public readonly string ModelName;

        /// <summary>
        /// The maximum number of concurrent requests that the server can handle.
        /// </summary>
        public uint MaxConcurrentRequests { get; }

        private LlamaCppClient(string modelName, uint maxConcurrency, LlamaServerHost? host, HttpClient client)
        {
            ModelName = modelName;
            MaxConcurrentRequests = maxConcurrency;
            _ownedHost = host;
            _httpClient = client;
        }

        /// <summary>
        /// Starts a new <see cref="LlamaCppClient"/> by starting a <see cref="LlamaServerHost"/> with the given options and logger.
        /// </summary>
        /// 
        /// <param name="options">
        /// The options to use when starting the <see cref="LlamaServerHost"/>.
        /// </param>
        /// 
        /// <param name="logger">
        /// The logger to use for logging messages from the <see cref="LlamaServerHost"/>.
        /// </param>
        /// 
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// 
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the started <see cref="LlamaCppClient"/>.
        /// </returns>
        /// <exception cref="ModelServerException">
        /// Thrown when the server process could not be started or did not become ready.
        /// </exception>
        public static async Task<LlamaCppClient> StartAsync(LlamaServerOptions options, ILogger logger, CancellationToken cancellationToken = default)
        {
            try
            {
                LlamaServerHost host = await LlamaServerHost.StartAsync(options, logger, cancellationToken).ConfigureAwait(false);
                return Attach(host, ownsHost: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or TimeoutException or HttpRequestException or IOException)
            {
                throw new ModelServerException($"The model server for '{Path.GetFileName(options.ModelPath)}' did not start: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Attaches to an existing <see cref="LlamaServerHost"/> and creates a <see cref="LlamaCppClient"/> that uses it.
        /// </summary>
        /// 
        /// <param name="host">
        /// The existing <see cref="LlamaServerHost"/> to attach to.
        /// </param>
        /// 
        /// <param name="ownsHost">
        /// </param>
        /// 
        /// <returns>
        /// A <see cref="LlamaCppClient"/> that uses the specified <see cref="LlamaServerHost"/>.
        /// </returns>
        public static LlamaCppClient Attach(LlamaServerHost host, bool ownsHost = false)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = host.BaseAddress,
                Timeout = TimeSpan.FromMinutes(10)
            };

            return new LlamaCppClient(host.ModelName, host.ParallelSlots, ownsHost ? host : null, client);
        }

        /// <summary>
        /// Disposes the <see cref="LlamaCppClient"/> and its owned <see cref="LlamaServerHost"/>, if any.
        /// </summary>
        /// 
        /// <param name="systemPrompt">
        /// The system prompt to use for the chat completion. 
        /// This is a message that sets the context for the conversation and can be used to instruct the model on how to behave.
        /// </param>
        /// 
        /// <param name="userPrompt">
        /// The user prompt to use for the chat completion.
        /// </param>
        /// 
        /// <param name="options">
        /// The options to use for the chat completion, such as temperature, max tokens, seed, and JSON schema.
        /// </param>
        /// 
        /// <param name="ctoken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// 
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the chat completion response from the server.
        /// </returns>
        /// 
        /// <exception cref="ModelServerException">
        /// Thrown on an infrastructure failure: the server is unreachable, timed out, or
        /// failed with a server error.
        /// </exception>
        ///
        /// <exception cref="HttpRequestException">
        /// Thrown when the server rejected the request itself, for example because it does
        /// not fit the context.
        /// </exception>
        public async Task<ChatCompletion> CompleteAsync(string systemPrompt, string userPrompt, ChatOptions options, CancellationToken ctoken = default)
        {
            try
            {
                return await SendAsync(systemPrompt, userPrompt, options, ctoken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (!ctoken.IsCancellationRequested)
            {
                throw new ModelServerException($"The model server did not respond in time: {ex.Message}", ex);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null || (int)ex.StatusCode >= 500)
            {
                throw new ModelServerException($"The model server failed: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new ModelServerException($"The connection to the model server broke: {ex.Message}", ex);
            }
        }

        private async Task<ChatCompletion> SendAsync(string systemPrompt, string userPrompt, ChatOptions options, CancellationToken ctoken)
        {
            Dictionary<string, object?> payload = new Dictionary<string, object?>
            {
                ["messages"] = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                ["temperature"] = options.Temperature,
                ["max_tokens"] = options.MaxTokens,
                ["seed"] = options.Seed
            };

            // If a schema was provided, add it to the payload so that the server enforces the format.
            // This way a malformed response will be impossible to get.
            if (options.JsonSchema is not null)
            {
                payload["response_format"] = new
                {
                    type = "json_schema",
                    json_schema = new
                    {
                        name = "score",
                        strict = true,
                        schema = JsonDocument.Parse(options.JsonSchema).RootElement
                    }
                };
            }

            // Needs the server's Jinja template engine, which the host always enables.
            if (options.ChatTemplateValues is { Count: > 0 } templateValues)
            {
                payload["chat_template_kwargs"] = templateValues;
            }

            // Send the request to the server and get the response.
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync("v1/chat/completions", payload, ctoken)
                .ConfigureAwait(false);

            // If the response is not successful, throw an exception with the status code and response body.
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ctoken).ConfigureAwait(false);
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {body}", null, response.StatusCode);
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(ctoken).ConfigureAwait(false);
            using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ctoken).ConfigureAwait(false);

            JsonElement choice = json.RootElement.GetProperty("choices")[0];
            JsonElement message = choice.GetProperty("message");

            return new ChatCompletion
            {
                Content = message.TryGetProperty("content", out JsonElement content) && content.ValueKind == JsonValueKind.String
                    ? content.GetString() ?? string.Empty
                    : string.Empty,
                ReasoningContent = message.TryGetProperty("reasoning_content", out JsonElement reasoning) && reasoning.ValueKind == JsonValueKind.String
                    ? reasoning.GetString()
                    : null,
                FinishReason = choice.TryGetProperty("finish_reason", out JsonElement finish) && finish.ValueKind == JsonValueKind.String
                    ? finish.GetString()
                    : null
            };
        }

        /// <summary>
        /// The llama.cpp build the server runs, such as <c>b6890-1a2b3c4</c>, or
        /// <see langword="null"/> when the server does not report it.
        /// </summary>
        public async Task<string?> GetServerVersionAsync(CancellationToken ctoken = default)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync("props", ctoken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                await using Stream stream = await response.Content.ReadAsStreamAsync(ctoken).ConfigureAwait(false);
                using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ctoken).ConfigureAwait(false);

                return json.RootElement.TryGetProperty("build_info", out JsonElement build) && build.ValueKind == JsonValueKind.String
                    ? build.GetString()
                    : null;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// The number of tokens <paramref name="text"/> takes for the loaded model.
        /// </summary>
        ///
        /// <exception cref="HttpRequestException"></exception>
        public async Task<int> CountTokensAsync(string text, CancellationToken ctoken = default)
        {
            try
            {
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync("tokenize", new { content = text }, ctoken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using Stream stream = await response.Content.ReadAsStreamAsync(ctoken).ConfigureAwait(false);
                using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: ctoken).ConfigureAwait(false);

                return json.RootElement.GetProperty("tokens").GetArrayLength();
            }
            catch (TaskCanceledException ex) when (!ctoken.IsCancellationRequested)
            {
                throw new ModelServerException($"The model server did not respond in time: {ex.Message}", ex);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                throw new ModelServerException($"The model server failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disposes the <see cref="LlamaCppClient"/> and its owned <see cref="LlamaServerHost"/>, if any.
        /// </summary>
        /// 
        /// <returns>
        /// Returns a <see cref="ValueTask"/> that represents the asynchronous dispose operation.
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            _httpClient.Dispose();
            if (_ownedHost is not null)
            {
                await _ownedHost.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
