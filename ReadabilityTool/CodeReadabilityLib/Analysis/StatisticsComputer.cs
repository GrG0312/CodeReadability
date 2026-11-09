using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Analysis.MetricImpact;
using CodeReadabilityLib.Analysis.ModelAxes;
using CodeReadabilityLib.Analysis.Output;
using CodeReadabilityLib.Analysis.Prompting;
using CodeReadabilityLib.Analysis.RunAgreement;
using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Reporting.Judgements;
using CodeReadabilityLib.Storage.Tracking;

namespace CodeReadabilityLib.Analysis
{
    /// <summary>
    /// Runs every analysis of <c>compute-statistics</c>: per-run agreement, model axes,
    /// prompting strategies and metric impact.
    /// </summary>
    public sealed class StatisticsComputer
    {
        private readonly AnalysisOptions _options;
        private readonly ILogger _logger;

        public StatisticsComputer(AnalysisOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <param name="provenance">
        /// Recorded in the returned section.
        /// </param>
        public StatisticsSection Compute(StatisticsInputs inputs, Provenance provenance)
        {
            List<string> warnings = new List<string>();
            JudgementReport judgements = inputs.Judgements;

            if (judgements.LlamaCppVersion is string judgedWith
                && inputs.Metrics.LlamaCppVersion is string gradedWith
                && judgedWith != gradedWith)
            {
                warnings.Add($"The judgements were made with llama.cpp {judgedWith} but the metrics with {gradedWith}.");
            }

            Dictionary<string, ModelDescription> descriptions =
                new Dictionary<string, ModelDescription>(judgements.ModelDescriptions, StringComparer.OrdinalIgnoreCase);

            List<AnalysedRun> runs = new List<AnalysedRun>();
            foreach (IGrouping<string, JudgedRun> group in judgements.Runs.GroupBy(r => r.RunName))
            {
                if (group.Count() > 1)
                {
                    warnings.Add($"Run '{group.Key}' appears {group.Count()} times; only the first was analysed.");
                }

                JudgedRun run = group.First();
                runs.Add(new AnalysedRun(run, descriptions[run.ModelName]));
            }

            Dictionary<string, double> reference = ReferenceScores(judgements);
            double threshold = AgreementCalculator.ReadableThreshold(judgements.Runs);

            _logger.Info($"Computing agreement statistics with confidence intervals for {runs.Count} run(s)...");
            List<RunSummary> summaries = new RunSummarizer(_options).Summarise(runs, reference, threshold);

            PairedComparer comparer = new PairedComparer(reference, _options);

            _logger.Info("Comparing models along the quantization, size, reasoning and family axes...");
            List<AxisComparison> axes = new ModelAxisAnalyzer(comparer).Analyze(runs);

            _logger.Info("Comparing prompting strategies...");
            Dictionary<string, double?> spearmanByRun = summaries.ToDictionary(
                s => $"{s.ModelName} [{s.PromptingStrategy}]",
                s => s.Statistics.Spearman);
            PromptingStrategyReport? strategies = new PromptingStrategyAnalyzer(comparer, _options, spearmanByRun).Analyze(runs);
            if (strategies is null)
            {
                warnings.Add("Only one prompting strategy was run, so strategies could not be compared.");
            }

            _logger.Info("Measuring metric impact...");
            MetricImpact.Results.MetricImpactReport metricImpact =
                new MetricImpactAnalyzer(_options, _logger).Analyze(runs, reference, inputs.Metrics);

            return new StatisticsSection
            {
                Provenance = provenance,
                Settings = new AnalysisSettings
                {
                    BootstrapIterations = _options.BootstrapIterations,
                    Alpha = _options.Alpha,
                    Seed = _options.Seed,
                    ReadableThreshold = AnalysisFormat.Finite(threshold)
                },
                Runs = summaries,
                ModelAxes = axes,
                PromptingStrategies = strategies,
                MetricImpact = metricImpact,
                Warnings = warnings
            };
        }

        /// <summary>
        /// The human score of every judged snippet, taken from the answers.
        /// </summary>
        private static Dictionary<string, double> ReferenceScores(JudgementReport judgements)
        {
            Dictionary<string, double> reference = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (ConcreteAnswer answer in judgements.Runs.SelectMany(r => r.ConcreteAnswers))
            {
                reference.TryAdd(answer.SnippetId, answer.OriginalScore);
            }

            return reference;
        }
    }
}
