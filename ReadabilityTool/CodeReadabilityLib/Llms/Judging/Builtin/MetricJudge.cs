using CodeReadabilityLib.Llms.Chatting;
using CodeReadabilityLib.Metrics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CodeReadabilityLib.Llms.Judging.Builtin
{
    /// <summary>
    /// A judge that grades every LLM-based metric of a snippet in one request, with a single
    /// fixed prompt and no prompting strategies.
    /// </summary>
    public sealed partial class MetricJudge : IReadabilityJudge
    {
        /// <summary>
        /// Represents the score value returned by the metric judge.
        /// </summary>
        private sealed record ScoreItem
        {
            [JsonPropertyName("id")]
            public string? Id { get; init; }

            [JsonPropertyName("score")]
            public int Score { get; init; }
        }

        /// <summary>
        /// Represents a list of score items returned by the metric judge.
        /// </summary>
        private sealed record ScoreList
        {
            [JsonPropertyName("scores")]
            public List<ScoreItem>? Scores { get; init; }
        }

        /// <summary>
        /// The JSON serializer options used for serializing and deserializing the metric judge configuration.
        /// </summary>
        private static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions(JsonSerializerDefaults.Web);


        /// <summary>
        /// The name of the metric judge, which is used to identify the judge in the system.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The chat client used to communicate with the LLM for evaluating code readability metrics.
        /// </summary>
        private readonly IChatClient _chatClient;

        private readonly uint _thinkingTokenBudget;
        private readonly IReadOnlyDictionary<string, JsonElement>? _chatTemplateValues;

        /// <summary>
        /// A reasoning model's thinking block, when the server leaves it in the content.
        /// </summary>
        [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex ThinkBlockRegex();

        /// <param name="thinkingTokenBudget">
        /// Extra completion tokens for a model's thinking. Zero for runs known not to think.
        /// </param>
        ///
        /// <param name="chatTemplateValues">
        /// Values sent to the model's chat template with every request, such as a hybrid
        /// model's thinking switch.
        /// </param>
        public MetricJudge(
            string name,
            IChatClient chatClient,
            uint thinkingTokenBudget = 0,
            IReadOnlyDictionary<string, JsonElement>? chatTemplateValues = null)
        {
            Name = name;
            _chatClient = chatClient;
            _thinkingTokenBudget = thinkingTokenBudget;
            _chatTemplateValues = chatTemplateValues;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException"></exception>
        public async Task<JudgementResult> EvaluateAsync(ReadabilityEvaluationRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Metrics.Count == 0)
            {
                throw new ArgumentException("At least one metric must be requested.", nameof(request));
            }

            // Construct the necessary parameters for the request
            string systemPrompt = BuildSystemPrompt();
            string userPrompt = BuildUserPrompt(request);
            uint tokensAllowed = 1024 + _thinkingTokenBudget;

            ChatOptions options = ChatOptions.Deterministic with
            {
                MaxTokens = tokensAllowed,
                JsonSchema = BuildSchema(request),
                ChatTemplateValues = _chatTemplateValues
            };

            ChatCompletion completion;
            try
            {
                completion = await _chatClient.CompleteAsync(systemPrompt, userPrompt, options, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                // The server refused this request itself (server faults arrive as
                // ModelServerException instead); asking again would be refused again.
                return JudgementResult.Failed(FailureKinds.Rejected, ex.Message);
            }

            string raw = completion.Content;

            // Removed first: a thinking block can contain braces, which the JSON extractor
            // would otherwise mistake for the answer.
            string answer = ThinkBlockRegex().Replace(raw, string.Empty);

            if (!LlmJsonParser.TryParse(answer, JSON_OPTIONS, out ScoreList? scoreList) || scoreList?.Scores is null)
            {
                return completion.FinishReason == "length"
                    ? JudgementResult.Failed(FailureKinds.CutOff, $"The reply was cut off after {tokensAllowed} tokens before it was complete.", raw)
                    : JudgementResult.Failed(FailureKinds.NoScore, "Response contained no parsable {\"scores\": [...]} object.", raw);
            }

            Dictionary<string, MetricDefinition> requested = request.Metrics.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);

            List<MetricScore> scores = new List<MetricScore>();

            foreach (ScoreItem item in scoreList.Scores)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                // Ignore hallucinated metric ids instead of recording them as real results.
                if (!requested.TryGetValue(item.Id.Trim(), out MetricDefinition? definition))
                {
                    continue;
                }

                scores.Add(MetricScore.FromDefinition(definition, item.Score));
            }

            if (scores.Count == 0)
            {
                return JudgementResult.Failed(FailureKinds.NoScore, "Response contained no recognised metric ids.", raw);
            }
            else
            {
                return JudgementResult.Ok(scores, raw);
            }
        }

        /// <summary>
        /// Builds the system prompt for the LLM, instructing it to evaluate code readability metrics in a strict and deterministic manner.
        /// </summary>
        private static string BuildSystemPrompt()
        {
            return
                "You are a strict, deterministic code readability evaluator. " +
                "Return ONLY compact JSON matching {\"scores\":[{\"id\":string,\"score\":int}]}. " +
                "Use exactly the metric ids you are given, score every one of them, and respect each metric's stated range. " +
                "No explanations, no markdown, no extra fields.";
        }

        /// <summary>
        /// Builds the user prompt for the LLM, providing it with the code to evaluate, the context, and the list of metrics to assess.
        /// </summary>
        /// 
        /// <param name="request">
        /// The readability evaluation request containing the code, context, and metrics to evaluate.
        /// </param>
        private static string BuildUserPrompt(ReadabilityEvaluationRequest request)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Evaluate the following code for readability across the listed metrics.").Append('\n');

            // If the language is specified, include it in the prompt.
            if (!string.IsNullOrWhiteSpace(request.Language))
            {
                sb.Append($"Language: {request.Language}").Append('\n');
            }

            // If the context is specified, include it in the prompt.
            if (!string.IsNullOrWhiteSpace(request.Context))
            {
                sb.Append($"Context: {request.Context}").Append('\n');
            }

            sb.Append('\n').Append("Metrics:").Append('\n');
            foreach (MetricDefinition metric in request.Metrics)
            {
                // This constructs a line for each metric that looks like this:
                // - [metricId] Metric Name: Metric Description
                sb
                    .Append("- [")
                    .Append(metric.Id)
                    .Append("] ")
                    .Append(metric.Name)
                    .Append(": ")
                    .Append(metric.Description);

                // This extends the line to include this at the end:
                // (range min-max, higher is better/worse)
                sb
                    .Append(" (range ")
                    .Append(metric.MinValue ?? 0)
                    .Append('-')
                    .Append(metric.MaxValue ?? 100)
                    .Append(", higher is ")
                    .Append(metric.HigherIsBetter ? "better" : "worse")
                    .Append(")").Append('\n');
            }

            // After the metrics, include the code to be evaluated in a fenced block
            sb
                .Append('\n')
                .Append("Code:").Append('\n')
                .Append("-----BEGIN-CODE-----").Append('\n')
                .Append(request.Code).Append('\n')
                .Append("-----END-CODE-----").Append('\n')
                .Append('\n')
                .Append("Return only the JSON object.").Append('\n');

            return sb.ToString();
        }

        /// <summary>
        /// Builds the JSON schema for the LLM response, ensuring that it matches the expected structure of scores for the requested metrics.
        /// </summary>
        /// 
        /// <param name="request">
        /// The readability evaluation request containing the metrics for which the schema should be built.
        /// </param>
        private static string BuildSchema(ReadabilityEvaluationRequest request)
        {
            string ids = string.Join(',', request.Metrics.Select(m => JsonSerializer.Serialize(m.Id)));
            return $$"""
            {
              "type": "object",
              "properties": {
                "scores": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "id": { "enum": [{{ids}}] },
                      "score": { "type": "integer", "minimum": 0, "maximum": 100 }
                    },
                    "required": ["id", "score"],
                    "additionalProperties": false
                  }
                }
              },
              "required": ["scores"],
              "additionalProperties": false
            }
            """;
        }
    }
}
