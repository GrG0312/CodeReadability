using CodeReadabilityLib.Llms.Judging;
using CodeReadabilityLib.Llms.Judging.Builtin;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Llms.ServerHosting;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Metrics;
using CodeReadabilityLib.Reporting.MetricResults;
using CodeReadabilityLib.Sources;
using CodeReadabilityLib.Storage.Tracking;
using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Pipelines.Shared;
using System.Text.Json;

namespace CodeReadabilityLib.Pipelines.ComputeMetrics
{
    /// <summary>
    /// Computes the readability metrics of every snippet of a dataset: the algorithmic ones
    /// from the code, and the LLM-based ones graded by every model in a folder.
    /// </summary>
    ///
    /// <remarks>
    /// Human scores are not needed, so unscored snippets are included. Each model file grades
    /// once; a hybrid model grades with thinking off, because metric grading is not what the
    /// thinking comparison is about.
    /// </remarks>
    public sealed class ComputeMetricsPipeline
    {
        private readonly ICodeSampleSource _source;
        private readonly ComputeMetricsOptions _options;
        private readonly ILogger _logger;

        public ComputeMetricsPipeline(ICodeSampleSource source, ComputeMetricsOptions options, ILogger logger)
        {
            _source = source;
            _options = options;
            _logger = logger;
        }

        /// <exception cref="InvalidOperationException">
        /// Thrown when the source yields no samples.
        /// </exception>
        ///
        /// <exception cref="FileNotFoundException">
        /// Thrown when LLM-based metrics are requested and the model folder has no
        /// <c>.gguf</c> files.
        /// </exception>
        public async Task<MetricsReport> RunAsync(Provenance provenance, CancellationToken cancellationToken = default)
        {
            List<CodeSample> samples = new List<CodeSample>();
            await foreach (CodeSample sample in _source.GetSamplesAsync(cancellationToken).ConfigureAwait(false))
            {
                samples.Add(sample);
            }

            if (samples.Count == 0)
            {
                throw new InvalidOperationException($"No samples were loaded from {_source.Description}.");
            }

            Dictionary<string, SnippetMetrics> snippets = samples
                .DistinctBy(sample => sample.Id)
                .ToDictionary(sample => sample.Id, sample => new SnippetMetrics { Id = sample.Id });

            MetricsReport report = new MetricsReport
            {
                Provenance = provenance,
                DatasetDescription = _source.Description,
                TotalSnippets = snippets.Count,
                MetricKinds = _options.Kinds
            };

            if (MetricKinds.IncludesAlgorithmic(_options.Kinds))
            {
                ComputeAlgorithmic(samples, snippets, report);
            }

            if (MetricKinds.IncludesLlm(_options.Kinds))
            {
                await GradeWithModelsAsync(samples, snippets, report, cancellationToken).ConfigureAwait(false);
            }

            report.Snippets.AddRange(snippets.Values.OrderBy(snippet => snippet.Id, StringComparer.Ordinal));
            return report;
        }

        private void ComputeAlgorithmic(IReadOnlyList<CodeSample> samples, Dictionary<string, SnippetMetrics> snippets, MetricsReport report)
        {
            report.AlgorithmicMetrics.AddRange(MetricRegistry.GetAlgorithmicForLanguage(_options.Language.Language).Select(metric => metric.Id));
            _logger.Info($"Computing {report.AlgorithmicMetrics.Count} algorithmic metric(s) for {snippets.Count} snippet(s)...");

            AlgorithmicEvaluator evaluator = new AlgorithmicEvaluator(_logger);
            foreach (CodeSample sample in samples.DistinctBy(sample => sample.Id))
            {
                foreach (MetricScore score in evaluator.Evaluate(sample.Code, sample.Language))
                {
                    if (double.IsFinite(score.Value))
                    {
                        snippets[sample.Id].Algorithmic[score.MetricId] = score.Value;
                    }
                }
            }
        }

        private async Task GradeWithModelsAsync(
            IReadOnlyList<CodeSample> samples,
            Dictionary<string, SnippetMetrics> snippets,
            MetricsReport report,
            CancellationToken cancellationToken)
        {
            if (_options.ModelDirectory is null || _options.ServerTemplate is null)
            {
                throw new InvalidOperationException("LLM-based metrics need a model folder and server settings.");
            }

            string[] modelPaths = Directory.GetFiles(_options.ModelDirectory, "*.gguf", SearchOption.TopDirectoryOnly);
            if (modelPaths.Length == 0)
            {
                throw new FileNotFoundException($"No .gguf files found in directory: {_options.ModelDirectory}");
            }

            Array.Sort(modelPaths, StringComparer.OrdinalIgnoreCase);

            List<MetricDefinition> metrics = MetricRegistry.GetLlmBasedForLanguage(_options.Language.Language).ToList();
            report.LlmMetrics.AddRange(metrics.Select(metric => metric.Id));

            _logger.Info($"Grading {metrics.Count} LLM-based metric(s) with {modelPaths.Length} model(s) over {snippets.Count} snippet(s).");

            List<CodeSample> unique = samples.DistinctBy(sample => sample.Id).ToList();

            foreach (string modelPath in modelPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string modelFile = Path.GetFileNameWithoutExtension(modelPath);
                if (_options.OnlyModel is string only && modelFile != only)
                {
                    continue;
                }

                report.Graders.Add(modelFile);

                await GradeWithModelAsync(modelPath, modelFile, metrics, unique, snippets, report, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The checkpoint unit of one grader model.
        /// </summary>
        public static string UnitId(string modelFile)
        {
            return $"grade__{modelFile}";
        }

        /// <summary>
        /// Grades every snippet with one model, restarting the server if it fails, and
        /// copies the saved grades into the report.
        /// </summary>
        private async Task GradeWithModelAsync(
            string modelPath,
            string modelFile,
            IReadOnlyList<MetricDefinition> metrics,
            IReadOnlyList<CodeSample> samples,
            Dictionary<string, SnippetMetrics> snippets,
            MetricsReport report,
            CancellationToken cancellationToken)
        {
            CheckpointStore store = _options.Checkpoints
                ?? throw new InvalidOperationException("LLM-based metrics need a checkpoint store.");
            string unit = UnitId(modelFile);

            if (store.IsDone(unit))
            {
                _logger.Info($"=== {modelFile} === already graded; using the saved grades.");
            }
            else
            {
                string? version = await GradeUnitAsync(modelPath, modelFile, metrics, samples, unit, cancellationToken).ConfigureAwait(false);
                report.LlamaCppVersion ??= version;
            }

            report.LlamaCppVersion ??= store.Plan.LlamaCppVersion;

            List<MetricGradeEntry> entries = store.ReadEntries<MetricGradeEntry>(unit)
                .GroupBy(entry => entry.SnippetId, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToList();

            List<MetricGradingFailure> failures = new List<MetricGradingFailure>();
            foreach (MetricGradeEntry entry in entries)
            {
                if (entry.Grades is not null)
                {
                    if (snippets.TryGetValue(entry.SnippetId, out SnippetMetrics? snippet))
                    {
                        snippet.Llm[modelFile] = entry.Grades;
                    }
                }
                else
                {
                    failures.Add(new MetricGradingFailure
                    {
                        SnippetId = entry.SnippetId,
                        ModelFile = modelFile,
                        Reason = entry.FailureReason ?? "Unknown failure."
                    });
                }
            }

            report.Failures.AddRange(failures.OrderBy(failure => failure.SnippetId, StringComparer.Ordinal));

            if (failures.Count > 0)
            {
                _logger.Warning($"{modelFile}: {failures.Count}/{samples.Count} snippet(s) could not be graded.");
            }
        }

        /// <summary>
        /// Starts one model and grades every snippet without a saved grading, saving each
        /// grading as it arrives.
        /// </summary>
        ///
        /// <returns>
        /// The llama.cpp build the server reported.
        /// </returns>
        private Task<string?> GradeUnitAsync(
            string modelPath,
            string modelFile,
            IReadOnlyList<MetricDefinition> metrics,
            IReadOnlyList<CodeSample> samples,
            string unit,
            CancellationToken cancellationToken)
        {
            CheckpointStore store = _options.Checkpoints!;
            ModelDescription description = _options.Descriptions.GetValueOrDefault(modelFile) ?? new ModelDescription();

            // A hybrid model grades with thinking off; a flash model never thinks.
            IReadOnlyDictionary<string, JsonElement>? templateValues = description.IsHybrid
                ? (description.ThinkingSwitch ?? ThinkingSwitch.Default).Off
                : null;
            bool thinks = !description.IsHybrid && ModelTypes.Normalize(description.Type) != ModelTypes.Flash;

            _logger.Info($"=== {modelFile} ==={(description.IsHybrid ? " (hybrid, thinking off)" : string.Empty)}");

            LlamaServerOptions serverOptions = _options.ServerTemplate! with
            {
                ModelPath = modelPath,
                ParallelSlots = (ushort)Math.Max(1, _options.Concurrency)
            };

            return ModelSession.RunAsync(serverOptions, store, _logger, async client =>
            {
                // One attempt within the assigned tokens: a failed grading is a result.
                IReadabilityJudge judge = new MetricJudge(modelFile, client, thinks ? _options.ThinkingTokenBudget : 0, templateValues);

                // Twice at most: the second pass only grades snippets whose saved grading was
                // lost (for example glued onto a line damaged by a crash).
                for (int pass = 0; pass < 2; pass++)
                {
                    await GradeRemainingAsync(judge, modelFile, metrics, samples, unit, cancellationToken).ConfigureAwait(false);
                }

                store.MarkDone(unit);
            }, cancellationToken);
        }

        /// <summary>
        /// Grades every snippet of the unit that has no saved grading yet.
        /// </summary>
        private async Task GradeRemainingAsync(
            IReadabilityJudge judge,
            string modelFile,
            IReadOnlyList<MetricDefinition> metrics,
            IReadOnlyList<CodeSample> samples,
            string unit,
            CancellationToken cancellationToken)
        {
            CheckpointStore store = _options.Checkpoints!;

            HashSet<string> graded = store.ReadEntries<MetricGradeEntry>(unit)
                .Select(entry => entry.SnippetId)
                .ToHashSet(StringComparer.Ordinal);
            List<CodeSample> remaining = samples.Where(sample => !graded.Contains(sample.Id)).ToList();
            if (remaining.Count == 0)
            {
                return;
            }

            if (graded.Count > 0)
            {
                _logger.Info($"  {modelFile}: {graded.Count} grading(s) already saved, {remaining.Count} to go.");
            }

            int completed = graded.Count;

            await using (JsonlCheckpointWriter writer = store.OpenWriter(unit))
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
                        ReadabilityEvaluationRequest request = new ReadabilityEvaluationRequest
                        {
                            Code = sample.Code,
                            Language = sample.Language.Language.ToString(),
                            Metrics = metrics
                        };

                        JudgementResult result = await judge.EvaluateAsync(request, token).ConfigureAwait(false);

                        await writer.AppendAsync(new MetricGradeEntry
                        {
                            SnippetId = sample.Id,
                            Grades = result.Succeeded ? result.Scores.ToDictionary(score => score.MetricId, score => score.Value) : null,
                            FailureReason = result.Succeeded ? null : result.FailureReason
                        }, token).ConfigureAwait(false);

                        int done = Interlocked.Increment(ref completed);
                        if (done % 10 == 0 || done == samples.Count)
                        {
                            _logger.Info($"  {modelFile}: {done}/{samples.Count}");
                        }
                    }).ConfigureAwait(false);
            }
        }
    }
}
