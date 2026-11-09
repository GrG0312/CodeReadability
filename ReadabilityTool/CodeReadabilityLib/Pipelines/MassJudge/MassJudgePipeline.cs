using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Llms.Judging;
using CodeReadabilityLib.Llms.Judging.Builtin;
using CodeReadabilityLib.Llms.Judging.Prompting;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Llms.ServerHosting;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Pipelines.Shared;
using CodeReadabilityLib.Reporting.Judgements;
using CodeReadabilityLib.Sources;
using CodeReadabilityLib.Storage.Tracking;

namespace CodeReadabilityLib.Pipelines.MassJudge
{
    /// <summary>
    /// Scores a labelled dataset with every GGUF model in a directory under every selected
    /// prompting strategy, and records every answer. Computes no statistics.
    /// </summary>
    ///
    /// <remarks>
    /// Models are loaded strictly one at a time; each server is used for every pass and
    /// strategy before the next model is loaded. Every answer is saved to the checkpoint
    /// store as it arrives, and work the store already holds is skipped, so the same call
    /// continues an interrupted execution.
    /// </remarks>
    public sealed class MassJudgePipeline
    {
        private readonly ICodeSampleSource _source;
        private readonly MassJudgeOptions _options;
        private readonly ILogger _logger;

        public MassJudgePipeline(ICodeSampleSource source, MassJudgeOptions options, ILogger logger)
        {
            _source = source;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Runs every discovered model, pass and strategy.
        /// </summary>
        ///
        /// <param name="provenance">
        /// Recorded in the returned file.
        /// </param>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the source yields no samples, or none of them carry a reference score.
        /// </exception>
        ///
        /// <exception cref="ResumableStopException">
        /// Thrown when a model server kept failing; the execution can be resumed.
        /// </exception>
        public async Task<JudgementReport> RunAsync(Provenance provenance, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CodeSample> samples = await CollectLabelledSamplesAsync(cancellationToken).ConfigureAwait(false);

            string[] modelPaths = Directory.GetFiles(_options.ModelDirectory, "*.gguf", SearchOption.TopDirectoryOnly);
            if (modelPaths.Length == 0)
            {
                throw new FileNotFoundException($"No .gguf files found in directory: {_options.ModelDirectory}");
            }

            // Sorted so that two runs over the same directory process models in the same order.
            Array.Sort(modelPaths, StringComparer.OrdinalIgnoreCase);

            if (_options.OnlyModel is string only)
            {
                modelPaths = modelPaths.Where(path => Path.GetFileNameWithoutExtension(path) == only).ToArray();
            }

            _logger.Info(
                $"Judging with {modelPaths.Length} model file(s) x {_options.Strategies.Count} prompting strategy(ies) " +
                $"over {samples.Count} snippet(s).");

            JudgementReport report = new JudgementReport
            {
                Provenance = provenance,
                DatasetDescription = _source.Description,
                TotalSnippets = samples.Count
            };

            foreach (string modelPath in modelPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string modelFile = Path.GetFileNameWithoutExtension(modelPath);
                ModelDescription description = _options.Descriptions.GetValueOrDefault(modelFile) ?? new ModelDescription();
                IReadOnlyList<JudgingPass> passes = JudgingPass.For(modelFile, description, _options.ThinkingTokenBudget);

                foreach (JudgingPass pass in passes)
                {
                    report.ModelDescriptions[pass.ModelName] = pass.Description;
                }

                if (passes.All(pass => _options.Strategies.All(strategy => _options.Checkpoints.IsDone(UnitId(pass, strategy)))))
                {
                    _logger.Info($"=== {modelFile} === already judged; using the saved answers.");
                }
                else
                {
                    string? version = await JudgeModelAsync(modelPath, passes, samples, cancellationToken).ConfigureAwait(false);
                    report.LlamaCppVersion ??= version;
                }

                foreach (JudgingPass pass in passes)
                {
                    foreach (IPromptingStrategy strategy in _options.Strategies)
                    {
                        report.Runs.Add(new JudgedRun
                        {
                            ModelName = pass.ModelName,
                            PromptingStrategy = strategy.Id,
                            ConcreteAnswers = SavedAnswers(UnitId(pass, strategy))
                        });
                    }
                }
            }

            report.LlamaCppVersion ??= _options.Checkpoints.Plan.LlamaCppVersion;
            return report;
        }

        /// <summary>
        /// The checkpoint unit of one pass under one strategy.
        /// </summary>
        public static string UnitId(JudgingPass pass, IPromptingStrategy strategy)
        {
            return $"judge__{pass.ModelName}__{strategy.Id}";
        }

        /// <summary>
        /// Drains the sample source and keeps only the samples that carry a reference score.
        /// </summary>
        private async Task<IReadOnlyList<CodeSample>> CollectLabelledSamplesAsync(CancellationToken cancellationToken)
        {
            List<CodeSample> all = new List<CodeSample>();
            await foreach (CodeSample sample in _source.GetSamplesAsync(cancellationToken).ConfigureAwait(false))
            {
                all.Add(sample);
            }

            if (all.Count == 0)
            {
                throw new InvalidOperationException($"No samples were loaded from {_source.Description}.");
            }

            // Judging is compared against ground truth later, so an unlabelled sample has
            // nothing to be compared with.
            List<CodeSample> labelled = all.Where(s => s.ReferenceScore.HasValue).ToList();

            if (labelled.Count != all.Count)
            {
                _logger.Warning($"{all.Count - labelled.Count} sample(s) had no reference score and were skipped.");
            }

            if (labelled.Count == 0)
            {
                throw new InvalidOperationException("Judging requires samples with reference scores.");
            }

            return labelled;
        }

        /// <summary>
        /// Starts one model and scores every unfinished pass and strategy with it,
        /// restarting the server if it fails.
        /// </summary>
        ///
        /// <returns>
        /// The llama.cpp build the server reported.
        /// </returns>
        private Task<string?> JudgeModelAsync(
            string modelPath,
            IReadOnlyList<JudgingPass> passes,
            IReadOnlyList<CodeSample> samples,
            CancellationToken cancellationToken)
        {
            ModelDescription description = passes[0].Description;
            _logger.Info(
                $"=== {Path.GetFileNameWithoutExtension(modelPath)} === (family {description.Family}, size {description.Size}, " +
                $"quant {description.Quant}, {(passes.Count > 1 ? ModelTypes.Hybrid : description.Type)})");

            LlamaServerOptions serverOptions = _options.ServerTemplate with
            {
                ModelPath = modelPath,
                ParallelSlots = (ushort)Math.Max(1, _options.Concurrency)
            };

            return ModelSession.RunAsync(serverOptions, _options.Checkpoints, _logger, async client =>
            {
                foreach (JudgingPass pass in passes)
                {
                    foreach (IPromptingStrategy strategy in _options.Strategies)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string unit = UnitId(pass, strategy);
                        if (_options.Checkpoints.IsDone(unit))
                        {
                            continue;
                        }

                        // One attempt within the assigned tokens: a failed answer is a result.
                        IReadabilityJudge judge =
                            new PromptedRangeJudge(pass.ModelName, client, strategy, pass.ThinkingTokenBudget, pass.ChatTemplateValues);

                        await ScoreUnitAsync(judge, $"{pass.ModelName} [{strategy.Id}]", unit, samples, cancellationToken).ConfigureAwait(false);

                        // Marked done only once every snippet really has a saved answer.
                        if (SavedAnswers(unit).Count < samples.Count)
                        {
                            await ScoreUnitAsync(judge, $"{pass.ModelName} [{strategy.Id}]", unit, samples, cancellationToken).ConfigureAwait(false);
                        }

                        _options.Checkpoints.MarkDone(unit);
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Scores every sample of one unit that has no saved answer yet, saving each answer
        /// as it arrives.
        /// </summary>
        private async Task ScoreUnitAsync(
            IReadabilityJudge judge,
            string runName,
            string unit,
            IReadOnlyList<CodeSample> samples,
            CancellationToken cancellationToken)
        {
            HashSet<string> answered = _options.Checkpoints.ReadEntries<ConcreteAnswer>(unit)
                .Select(answer => answer.SnippetId)
                .ToHashSet(StringComparer.Ordinal);

            List<CodeSample> remaining = samples.Where(sample => !answered.Contains(sample.Id)).ToList();
            if (answered.Count > 0)
            {
                _logger.Info($"  {runName}: {answered.Count} answer(s) already saved, {remaining.Count} to go.");
            }

            int completed = answered.Count;

            await using (JsonlCheckpointWriter writer = _options.Checkpoints.OpenWriter(unit))
            {
                await Parallel.ForEachAsync(
                    remaining,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(1, _options.Concurrency),
                        CancellationToken = cancellationToken
                    },
                    async (CodeSample sample, CancellationToken token) =>
                    {
                        ConcreteAnswer answer = await ScoreSampleAsync(judge, sample, token).ConfigureAwait(false);
                        await writer.AppendAsync(answer, token).ConfigureAwait(false);

                        int done = Interlocked.Increment(ref completed);
                        if (done % 10 == 0 || done == samples.Count)
                        {
                            _logger.Info($"  {runName}: {done}/{samples.Count}");
                        }
                    }).ConfigureAwait(false);
            }

            List<ConcreteAnswer> answers = SavedAnswers(unit);
            int unusable = answers.Count(a => a.ModelAnswer is null);
            if (unusable > 0)
            {
                _logger.Warning($"{runName}: {unusable}/{answers.Count} response(s) were unusable.");
            }
        }

        /// <summary>
        /// A unit's saved answers, one per snippet, sorted by snippet id.
        /// </summary>
        private List<ConcreteAnswer> SavedAnswers(string unit)
        {
            return _options.Checkpoints.ReadEntries<ConcreteAnswer>(unit)
                .GroupBy(answer => answer.SnippetId, StringComparer.Ordinal)
                .Select(group => group.Last())
                .OrderBy(answer => answer.SnippetId, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Asks the judge to score one sample. A model failure is recorded as an answer
        /// without a score; an infrastructure failure propagates and is never recorded.
        /// </summary>
        private static async Task<ConcreteAnswer> ScoreSampleAsync(
            IReadabilityJudge judge,
            CodeSample sample,
            CancellationToken cancellationToken)
        {
            ReadabilityEvaluationRequest request = new ReadabilityEvaluationRequest
            {
                Code = sample.Code,
                Language = sample.Language.Language.ToString()
            };

            JudgementResult result = await judge.EvaluateAsync(request, cancellationToken).ConfigureAwait(false);

            return new ConcreteAnswer
            {
                SnippetId = sample.Id,
                OriginalScore = sample.ReferenceScore!.Value,
                ModelAnswer = result.Succeeded ? (int)result.Scores[0].Value : null,
                FailureKind = result.FailureKind,
                FailureReason = result.FailureReason
            };
        }
    }
}
