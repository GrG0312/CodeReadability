using CodeReadabilityLib.Analysis.Inputs;
using CodeReadabilityLib.Analysis.MetricImpact.Computation;
using CodeReadabilityLib.Analysis.MetricImpact.Results;
using CodeReadabilityLib.Analysis.MetricImpact.Values;
using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Logging;
using CodeReadabilityLib.Reporting.MetricResults;
using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.MetricImpact
{
    /// <summary>
    /// Which metrics the human and the model readability scores follow, how strongly, and
    /// where the models differ from the humans.
    /// </summary>
    ///
    /// <remarks>
    /// Every correlation is a Spearman correlation between a metric and a set of readability
    /// scores, on the snippets that have both. Rankings order metrics by the absolute
    /// correlation; the sign is reported as the direction. A model's correlation is pooled
    /// over its runs, and the overall correlation over the models, through the Fisher
    /// z-transform. One set of bootstrap resamples of the snippets is shared by every
    /// analysis, so that all of them are paired.
    /// </remarks>
    internal sealed class MetricImpactAnalyzer
    {
        private readonly AnalysisOptions _options;
        private readonly ILogger _logger;

        public MetricImpactAnalyzer(AnalysisOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        public MetricImpactReport Analyze(
            IReadOnlyList<AnalysedRun> runs,
            IReadOnlyDictionary<string, double> reference,
            MetricsReport metricsReport)
        {
            HashSet<string> withMetrics = metricsReport.Snippets.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
            List<string> snippetIds = reference.Keys.Where(withMetrics.Contains).OrderBy(id => id, StringComparer.Ordinal).ToList();

            MetricValueTable table = new MetricValueTable(metricsReport, snippetIds);
            IReadOnlyList<MetricInfo> metrics = table.Metrics;

            List<GraderAgreement> agreement = GraderAgreementCalculator.Compute(metricsReport);
            HashSet<string> lowAgreement = agreement.Where(a => a.Flagged).Select(a => a.MetricId).ToHashSet();

            RankingBuilder builder = new RankingBuilder(metrics, lowAgreement, _options);
            SnippetBootstrap bootstrap = new SnippetBootstrap(
                snippetIds.Count, _options.BootstrapIterations, Bootstrap.DeriveSeed(_options.Seed, "metric-impact"));

            double[] human = snippetIds.Select(id => reference[id]).ToArray();

            _logger.Info($"Metric impact: {metrics.Count} metric(s) over {snippetIds.Count} snippet(s), {runs.Count} run(s).");

            CorrelationSet humanSet = CorrelationSet.Compute(metrics.Select(table.ForHumans).ToList(), human, bootstrap);

            List<ModelMetricImpact> perModel = new List<ModelMetricImpact>();
            List<RunMetricImpact> perRun = new List<RunMetricImpact>();
            List<CorrelationSet> modelSets = new List<CorrelationSet>();

            foreach (IGrouping<string, AnalysedRun> model in runs.GroupBy(r => r.ModelName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                string modelFile = model.First().ModelFile;
                List<double[]> values = metrics.Select(metric => table.ForModel(metric, modelFile)).ToList();

                // The human side of every model-versus-human comparison uses the same
                // leave-one-out values as the model side.
                CorrelationSet humanForModel = CorrelationSet.Compute(values, human, bootstrap);

                List<(AnalysedRun Run, CorrelationSet Set)> runSets = model
                    .OrderBy(r => r.Strategy, StringComparer.Ordinal)
                    .Select(run => (run, CorrelationSet.Compute(values, Scores(run, snippetIds), bootstrap)))
                    .ToList();

                CorrelationSet modelSet = CorrelationSet.FisherMean(runSets.Select(r => r.Set).ToList());
                modelSets.Add(modelSet);

                Dictionary<string, double?> samePriorities = new Dictionary<string, double?>();
                foreach ((AnalysedRun run, CorrelationSet set) in runSets)
                {
                    MetricRanking ranking = builder.Build(set, humanForModel);
                    samePriorities[run.Strategy] = ranking.SamePrioritiesAsHumans;

                    perRun.Add(new RunMetricImpact
                    {
                        ModelName = run.ModelName,
                        PromptingStrategy = run.Strategy,
                        Ranking = ranking,
                        VersusHuman = builder.VersusHuman(set, humanForModel)
                    });
                }

                perModel.Add(new ModelMetricImpact
                {
                    ModelName = model.Key,
                    ModelFile = modelFile,
                    Ranking = builder.Build(modelSet, humanForModel),
                    VersusHuman = builder.VersusHuman(modelSet, humanForModel),
                    SamePrioritiesByStrategy = samePriorities,
                    ClosestStrategy = samePriorities.Where(p => p.Value.HasValue).OrderByDescending(p => p.Value).Select(p => p.Key).FirstOrDefault(),
                    SelfConsistency = SelfConsistency(model.ToList(), modelFile, table, snippetIds)
                });

                _logger.Info($"  {model.Key}: done.");
            }

            CorrelationSet overallSet = modelSets.Count > 0 ? CorrelationSet.FisherMean(modelSets) : humanSet;

            return new MetricImpactReport
            {
                MetricKinds = metricsReport.MetricKinds,
                Metrics = metrics.ToList(),
                Snippets = snippetIds.Count,
                Human = builder.Build(humanSet, null),
                Overall = builder.Build(overallSet, humanSet),
                PerModel = perModel,
                PerRun = perRun,
                GraderAgreement = agreement,
                Notes = Notes(metricsReport)
            };
        }

        /// <summary>
        /// A run's scores aligned with the snippets; NaN where its answer was unusable.
        /// </summary>
        private static double[] Scores(AnalysedRun run, IReadOnlyList<string> snippetIds)
        {
            return snippetIds.Select(id => run.Predictions.TryGetValue(id, out double score) ? score : double.NaN).ToArray();
        }

        /// <summary>
        /// Whether the model's readability scores follow its own grades of the LLM-based
        /// metrics. Only for models whose file graded the metrics.
        /// </summary>
        private static List<SelfConsistencyEntry> SelfConsistency(
            IReadOnlyList<AnalysedRun> runs,
            string modelFile,
            MetricValueTable table,
            IReadOnlyList<string> snippetIds)
        {
            if (!table.Graders.Contains(modelFile))
            {
                return new List<SelfConsistencyEntry>();
            }

            return table.Metrics
                .Where(metric => metric.IsLlm)
                .Select(metric =>
                {
                    double[] own = table.OwnGrades(metric.MetricId, modelFile);
                    double[] correlations = runs.Select(run => PairedSpearman.Compute(own, Scores(run, snippetIds))).ToArray();
                    return new SelfConsistencyEntry
                    {
                        MetricId = metric.MetricId,
                        Spearman = AnalysisFormat.Finite(Correlation.FisherMean(correlations)),
                        Runs = correlations.Count(double.IsFinite)
                    };
                })
                .ToList();
        }

        private static List<string> Notes(MetricsReport report)
        {
            List<string> notes =
            [
                "Rankings order metrics by the absolute Spearman correlation (strength); the sign of 'spearman' is the direction.",
                "A model's correlations are pooled over its runs, and the overall ones over the models, through the Fisher z-transform.",
                "Correlation is association, not use: metrics correlate with each other (lines of code, Halstead volume and cyclomatic complexity all grow with length), so a high correlation marks a cue the scores move with.",
                "Pairwise metric tests and model-versus-human tests are Holm-corrected within each ranking or model; neighbouring metrics will often not differ significantly."
            ];

            if (report.LlmMetrics.Count > 0)
            {
                notes.Add("LLM-based metric values are averages of the graders' grades: over all graders for the human scores, and over every grader except the analysed model's own file (leave-one-out) for a model.");
                notes.Add("An LLM-based metric with poor grader agreement (ICC < 0.5) is flagged: its values are mostly noise, so a low rank means 'not measured reliably' rather than 'unimportant'.");
            }

            if (!MetricKinds.IncludesLlm(report.MetricKinds))
            {
                notes.Add("The metrics file has no LLM-based metrics, so leave-one-out averaging, grader agreement and self-consistency were skipped.");
            }

            if (!MetricKinds.IncludesAlgorithmic(report.MetricKinds))
            {
                notes.Add("The metrics file has no algorithmic metrics; only LLM-based metrics were analysed.");
            }

            return notes;
        }
    }
}
