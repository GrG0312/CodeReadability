using CodeReadabilityLib.Llms.Chatting;
using CodeReadabilityLib.Llms.Judging.Prompting;
using CodeReadabilityLib.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CodeReadabilityLib.Llms.Judging.Builtin
{
    /// <summary>
    /// A judge that asks for a single overall readability score on the fixed scale,
    /// phrasing the request according to a prompting strategy.
    /// </summary>
    public sealed partial class PromptedRangeJudge : IReadabilityJudge
    {
        private sealed record ScoreItem
        {
            [JsonPropertyName("score")]
            public int? Score { get; init; }
        }

        public const string METRIC_ID = "overall_readability";

        private static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        /// <summary>
        /// The reply schema. llama.cpp compiles it into a grammar, so a reply that is not a
        /// score on the scale cannot be generated at all.
        /// </summary>
        private static readonly string ScoreSchema = $$"""
            {
            "type": "object",
            "properties": { "score": { "type": "integer", "minimum": {{ReadabilityScale.Min}}, "maximum": {{ReadabilityScale.Max}} } },
            "required": ["score"],
            "additionalProperties": false
            }
            """;

        /// <summary>
        /// A reasoning model's thinking block, when the server leaves it in the content.
        /// </summary>
        [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex ThinkBlockRegex();

        public string Name { get; }

        private readonly IChatClient _chatClient;
        private readonly IPromptingStrategy _strategy;
        private readonly uint _thinkingTokenBudget;
        private readonly IReadOnlyDictionary<string, JsonElement>? _chatTemplateValues;

        /// <param name="thinkingTokenBudget">
        /// Extra completion tokens for a model's thinking, which counts against the
        /// completion limit. Zero for runs known not to think.
        /// </param>
        ///
        /// <param name="chatTemplateValues">
        /// Values sent to the model's chat template with every request, such as a hybrid
        /// model's thinking switch.
        /// </param>
        public PromptedRangeJudge(
            string name,
            IChatClient chatClient,
            IPromptingStrategy strategy,
            uint thinkingTokenBudget = 0,
            IReadOnlyDictionary<string, JsonElement>? chatTemplateValues = null)
        {
            Name = name;
            _chatClient = chatClient;
            _strategy = strategy;
            _thinkingTokenBudget = thinkingTokenBudget;
            _chatTemplateValues = chatTemplateValues;
        }

        /// <inheritdoc/>
        public async Task<JudgementResult> EvaluateAsync(ReadabilityEvaluationRequest request, CancellationToken cancellationToken = default)
        {
            PromptSpecification prompt = _strategy.Build(request);

            uint tokensAllowed = prompt.MaxTokens + _thinkingTokenBudget;

            ChatOptions chatOptions = ChatOptions.Deterministic with
            {
                MaxTokens = tokensAllowed,
                JsonSchema = ScoreSchema,
                ChatTemplateValues = _chatTemplateValues
            };

            ChatCompletion completion;
            try
            {
                completion = await _chatClient
                    .CompleteAsync(prompt.SystemPrompt, prompt.UserPrompt, chatOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                // The server refused this request itself (server faults arrive as
                // ModelServerException instead); asking again would be refused again.
                return JudgementResult.Failed(FailureKinds.Rejected, ex.Message);
            }

            string rawResponse = completion.Content;

            // Removed first: a thinking block can contain braces, which the JSON extractor
            // would otherwise mistake for the answer.
            string answer = ThinkBlockRegex().Replace(rawResponse, string.Empty);

            if (!LlmJsonParser.TryParse(answer, JSON_OPTIONS, out ScoreItem? item) || item?.Score is not int score)
            {
                return completion.FinishReason == "length"
                    ? JudgementResult.Failed(FailureKinds.CutOff, $"The reply was cut off after {tokensAllowed} tokens before it contained a score.", rawResponse)
                    : JudgementResult.Failed(FailureKinds.NoScore, "Response contained no parsable {\"score\": int} object.", rawResponse);
            }

            if (score < ReadabilityScale.Min || score > ReadabilityScale.Max)
            {
                return JudgementResult.Failed(
                    FailureKinds.OutOfRange, $"Score {score} is out of range [{ReadabilityScale.Min}, {ReadabilityScale.Max}].", rawResponse);
            }

            return JudgementResult.Ok([MetricScore.Raw(METRIC_ID, score)], rawResponse);
        }
    }
}
