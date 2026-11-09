using CodeReadabilityLib.Llms.Models.Descriptions;
using CodeReadabilityLib.Reporting.Judgements;

namespace CodeReadabilityLib.Analysis.Inputs
{
    /// <summary>
    /// One run (one model, one prompting strategy) prepared for analysis.
    /// </summary>
    public sealed class AnalysedRun
    {
        public JudgedRun Run { get; }

        /// <summary>
        /// The run's model description, with every placeholder filled in.
        /// </summary>
        public ModelDescription Description { get; }

        /// <summary>
        /// The usable scores, keyed by snippet id.
        /// </summary>
        public IReadOnlyDictionary<string, double> Predictions { get; }

        public string ModelName => Run.ModelName;

        public string Strategy => Run.PromptingStrategy;

        public string RunName => Run.RunName;

        /// <summary>
        /// The model file the run came from. Both runs of a hybrid model share it.
        /// </summary>
        public string ModelFile => Description.ModelFile ?? Run.ModelName;

        public AnalysedRun(JudgedRun run, ModelDescription description)
        {
            Run = run;
            Description = description;

            Dictionary<string, double> predictions = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (ConcreteAnswer answer in run.ConcreteAnswers)
            {
                if (answer.ModelAnswer is int value)
                {
                    predictions[answer.SnippetId] = value;
                }
            }

            Predictions = predictions;
        }
    }
}
