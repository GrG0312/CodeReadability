using CodeReadabilityLib.Llms.Chatting;
using CodeReadabilityLib.Llms.Judging;
using CodeReadabilityLib.Llms.Judging.Builtin;
using CodeReadabilityLib.Llms.Judging.Prompting;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Llms.ServerHosting;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Pipelines.MassJudge;
using System.Diagnostics;

namespace CodeReadabilityLib.Preflight.Models
{
    /// <summary>
    /// Tests that a model works before a long run is committed to it: it loads, answers
    /// every prompting strategy, fits its longest request in the context, and, for a hybrid
    /// model, really switches its thinking on and off.
    /// </summary>
    ///
    /// <remarks>
    /// Shared by <c>check-models</c> and the up-front checks of <c>mass-judge</c> and
    /// <c>run-pipeline</c>, so that a broken model is caught before any judging even when
    /// <c>check-models</c> was not run.
    /// </remarks>
    public sealed class ModelChecker
    {
        /// <summary>
        /// Tokens allowed for the chat template's own text around the messages.
        /// </summary>
        private const int TemplateOverheadTokens = 64;

        /// <summary>
        /// A short question that makes a thinking model think.
        /// </summary>
        private const string ThinkingProbe = "Is 391 a prime number? Answer yes or no.";

        private readonly LlamaServerOptions _serverTemplate;
        private readonly IReadOnlyList<IPromptingStrategy> _strategies;
        private readonly uint _thinkingTokenBudget;
        private readonly int _concurrency;
        private readonly ILogger _logger;

        public ModelChecker(
            LlamaServerOptions serverTemplate,
            IReadOnlyList<IPromptingStrategy> strategies,
            uint thinkingTokenBudget,
            int concurrency,
            ILogger logger)
        {
            _serverTemplate = serverTemplate;
            _strategies = strategies;
            _thinkingTokenBudget = thinkingTokenBudget;
            _concurrency = Math.Max(1, concurrency);
            _logger = logger;
        }

        /// <param name="sampleCode">
        /// The code of the test request; normally a short snippet of the dataset.
        /// </param>
        ///
        /// <param name="longestCode">
        /// The dataset's longest snippet, used to check that every request fits the context.
        /// </param>
        public async Task<ModelCheckResult> CheckAsync(
            string modelPath,
            ModelDescription description,
            string sampleCode,
            string longestCode,
            string language,
            CancellationToken cancellationToken = default)
        {
            string modelFile = Path.GetFileNameWithoutExtension(modelPath);
            List<string> problems = new List<string>();
            IReadOnlyList<JudgingPass> passes = JudgingPass.For(modelFile, description, _thinkingTokenBudget);

            LlamaServerOptions options = _serverTemplate with { ModelPath = modelPath, ParallelSlots = (ushort)_concurrency };

            LlamaCppClient client;
            try
            {
                client = await LlamaCppClient.StartAsync(options, _logger, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new ModelCheckResult { ModelFile = modelFile, Loaded = false, Problems = [$"{modelFile}: failed to load: {ex.Message}"] };
            }

            await using (client.ConfigureAwait(false))
            {
                List<PassCheckResult> passResults = new List<PassCheckResult>();
                int largest;
                int perRequest;

                try
                {
                    foreach (JudgingPass pass in passes)
                    {
                        passResults.Add(await CheckPassAsync(client, pass, sampleCode, language, problems, cancellationToken).ConfigureAwait(false));
                    }

                    (largest, perRequest) = await LargestRequestAsync(client, passes, longestCode, language, cancellationToken).ConfigureAwait(false);
                }
                catch (ModelServerException ex)
                {
                    problems.Add($"{modelFile}: the model server failed during the check: {ex.Message}");
                    return new ModelCheckResult { ModelFile = modelFile, Loaded = true, Problems = problems, Passes = passResults };
                }
                if (largest > perRequest)
                {
                    problems.Add(
                        $"{modelFile}: the largest request needs about {largest} tokens (prompt, answer and thinking budget), " +
                        $"but only {perRequest} fit per request ({options.ContextSize} context / {_concurrency} parallel). " +
                        "Raise --ctx-size, lower --concurrency or --thinking-budget.");
                }

                return new ModelCheckResult
                {
                    ModelFile = modelFile,
                    Loaded = true,
                    Problems = problems,
                    Passes = passResults,
                    LargestRequestTokens = largest,
                    ContextPerRequest = perRequest
                };
            }
        }

        private async Task<PassCheckResult> CheckPassAsync(
            LlamaCppClient client,
            JudgingPass pass,
            string sampleCode,
            string language,
            List<string> problems,
            CancellationToken cancellationToken)
        {
            List<string> answered = new List<string>();
            Dictionary<string, string> failures = new Dictionary<string, string>();
            List<double> seconds = new List<double>();

            foreach (IPromptingStrategy strategy in _strategies)
            {
                // The same judge a real run uses, so that the check behaves exactly like judging.
                IReadabilityJudge judge = new PromptedRangeJudge(pass.ModelName, client, strategy, pass.ThinkingTokenBudget, pass.ChatTemplateValues);
                Stopwatch watch = Stopwatch.StartNew();

                JudgementResult result = await judge.EvaluateAsync(new ReadabilityEvaluationRequest { Code = sampleCode, Language = language }, cancellationToken)
                    .ConfigureAwait(false);

                // Failed answers count too: a real run spends this time on them as well.
                seconds.Add(watch.Elapsed.TotalSeconds);

                if (result.Succeeded)
                {
                    answered.Add(strategy.Id);
                }
                else if (result.FailureKind == FailureKinds.Rejected)
                {
                    // The server refusing the request is a setup problem, not a model limitation.
                    problems.Add($"{pass.ModelName} [{strategy.Id}]: the model server refused the test request: {result.FailureReason}");
                }
                else
                {
                    // A model failing to answer is a result that the statistics count, not a
                    // reason to stop the run.
                    failures[strategy.Id] = result.FailureKind ?? FailureKinds.Unspecified;
                }
            }

            bool? switchWorks = null;
            if (pass.ChatTemplateValues is not null)
            {
                switchWorks = await CheckThinkingSwitchAsync(client, pass, cancellationToken).ConfigureAwait(false);
                if (switchWorks == false)
                {
                    bool expected = ModelTypes.Normalize(pass.Description.Type) == ModelTypes.Reasoning;
                    problems.Add(
                        $"{pass.ModelName}: the thinking switch does not work: with thinking {(expected ? "on" : "off")} the reply " +
                        $"{(expected ? "showed no" : "still showed")} thinking. Check the model's thinking-switch in the description file.");
                }
            }

            return new PassCheckResult
            {
                ModelName = pass.ModelName,
                AnsweredStrategies = answered,
                FailedStrategies = failures,
                SecondsPerAnswer = seconds.Count > 0 ? seconds.Average() : null,
                ThinkingSwitchWorks = switchWorks
            };
        }

        /// <summary>
        /// Whether the reply shows thinking exactly when the pass switches thinking on.
        /// </summary>
        private async Task<bool> CheckThinkingSwitchAsync(LlamaCppClient client, JudgingPass pass, CancellationToken cancellationToken)
        {
            bool expectThinking = ModelTypes.Normalize(pass.Description.Type) == ModelTypes.Reasoning;

            ChatCompletion completion = await client.CompleteAsync(
                "You are a helpful assistant.",
                ThinkingProbe,
                ChatOptions.Deterministic with
                {
                    MaxTokens = 64 + (expectThinking ? _thinkingTokenBudget : 0),
                    ChatTemplateValues = pass.ChatTemplateValues
                },
                cancellationToken).ConfigureAwait(false);

            return completion.ShowsThinking == expectThinking;
        }

        /// <summary>
        /// The tokens the largest request needs, and the context one request may use.
        /// </summary>
        private async Task<(int Largest, int PerRequest)> LargestRequestAsync(
            LlamaCppClient client,
            IReadOnlyList<JudgingPass> passes,
            string longestCode,
            string language,
            CancellationToken cancellationToken)
        {
            ReadabilityEvaluationRequest request = new ReadabilityEvaluationRequest { Code = longestCode, Language = language };
            uint budget = passes.Max(pass => pass.ThinkingTokenBudget);

            int largest = 0;
            foreach (IPromptingStrategy strategy in _strategies)
            {
                PromptSpecification prompt = strategy.Build(request);
                int promptTokens = await client.CountTokensAsync(prompt.SystemPrompt + "\n" + prompt.UserPrompt, cancellationToken).ConfigureAwait(false);
                largest = Math.Max(largest, promptTokens + TemplateOverheadTokens + (int)prompt.MaxTokens + (int)budget);
            }

            // The server splits its context evenly between its parallel slots.
            int perRequest = (int)(_serverTemplate.ContextSize / (uint)_concurrency);
            return (largest, perRequest);
        }
    }
}
