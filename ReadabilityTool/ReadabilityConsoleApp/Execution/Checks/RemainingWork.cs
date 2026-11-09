using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Llms.Judging.Prompting;
using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Pipelines.ComputeMetrics;
using CodeReadabilityLib.Pipelines.MassJudge;
using ReadabilityConsoleApp.Cli;

namespace ReadabilityConsoleApp.Execution.Checks
{
    /// <summary>
    /// Which models an execution still has to run, so that a resumed or per-model job only
    /// checks and loads the models it will actually use.
    /// </summary>
    public static class RemainingWork
    {
        /// <param name="judges">
        /// Whether the execution judges readability (mass-judge).
        /// </param>
        ///
        /// <param name="grades">
        /// Whether the execution grades LLM-based metrics.
        /// </param>
        public static string[] Models(
            CommandLine args,
            IReadOnlyList<string> modelPaths,
            IReadOnlyDictionary<string, ModelDescription> descriptions,
            bool judges,
            bool grades)
        {
            IEnumerable<string> models = modelPaths;

            if (args.Optional("only-model") is string only)
            {
                models = models.Where(path => Path.GetFileNameWithoutExtension(path) == only);
            }

            if (args.Resume is CheckpointStore store)
            {
                IReadOnlyList<IPromptingStrategy> strategies = SharedOptions.Strategies(args);
                uint budget = SharedOptions.ThinkingBudget(args);

                models = models.Where(path =>
                {
                    string modelFile = Path.GetFileNameWithoutExtension(path);
                    ModelDescription description = descriptions.GetValueOrDefault(modelFile) ?? new ModelDescription();

                    bool judged = !judges || JudgingPass.For(modelFile, description, budget)
                        .All(pass => strategies.All(strategy => store.IsDone(MassJudgePipeline.UnitId(pass, strategy))));
                    bool graded = !grades || store.IsDone(ComputeMetricsPipeline.UnitId(modelFile));

                    return !(judged && graded);
                });
            }

            return models.ToArray();
        }
    }
}
