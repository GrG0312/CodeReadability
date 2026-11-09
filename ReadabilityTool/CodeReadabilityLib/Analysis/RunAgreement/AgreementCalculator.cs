using CodeReadabilityLib.Analysis.Shared;
using CodeReadabilityLib.Llms.Judging;
using CodeReadabilityLib.Reporting.Judgements;
using CodeReadabilityLib.Statistics;

namespace CodeReadabilityLib.Analysis.RunAgreement
{
    /// <summary>
    /// Derives a run's distance histogram and agreement statistics from its answers.
    /// </summary>
    public static class AgreementCalculator
    {
        /// <summary>
        /// The distance histogram of a run's usable answers. Index <c>i</c> counts answers
        /// whose distance from the human score, rounded down, is <c>i</c>; index 3 holds
        /// everything three or more points away.
        /// </summary>
        public static int[] DistanceHistogram(JudgedRun run)
        {
            int[] buckets = new int[4];
            foreach (ConcreteAnswer answer in run.ConcreteAnswers)
            {
                if (answer.ModelAnswer is int value)
                {
                    buckets[Math.Min(3, (int)Math.Abs(value - answer.OriginalScore))]++;
                }
            }

            return buckets;
        }

        /// <summary>
        /// The agreement statistics of a run's usable answers.
        /// </summary>
        ///
        /// <param name="aucThreshold">
        /// The human score above which a snippet counts as readable, normally
        /// <see cref="ReadableThreshold"/>.
        /// </param>
        public static AgreementStatistics Compute(JudgedRun run, double aucThreshold)
        {
            List<double> reference = new List<double>();
            List<double> predicted = new List<double>();

            foreach (ConcreteAnswer answer in run.ConcreteAnswers)
            {
                // Unusable answers contribute to no statistic.
                if (answer.ModelAnswer is int value)
                {
                    reference.Add(answer.OriginalScore);
                    predicted.Add(value);
                }
            }

            int evaluated = reference.Count;
            int total = run.ConcreteAnswers.Count;

            Dictionary<string, int> failures = run.ConcreteAnswers
                .Where(answer => answer.ModelAnswer is null)
                .GroupBy(answer => answer.FailureKind ?? FailureKinds.Unspecified)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count());

            // Spearman needs at least three pairs before a p-value is meaningful.
            SpearmanResult? spearman = evaluated >= 3 ? Spearman.Compute(reference, predicted) : null;
            AgreementSnapshot? snapshot = evaluated == 0
                ? null
                : AgreementSnapshot.Compute(reference, predicted, ReadabilityScale.Min, ReadabilityScale.Max, aucThreshold);

            return new AgreementStatistics
            {
                Evaluated = evaluated,
                Unusable = total - evaluated,
                AnswerRate = total == 0 ? null : (double)evaluated / total,
                Failures = failures,
                Spearman = AnalysisFormat.Finite(spearman?.Rho),
                PValue = AnalysisFormat.Finite(spearman?.PValue),
                Pearson = evaluated < 3 ? null : AnalysisFormat.Finite(snapshot?.Pearson),
                KendallTau = evaluated < 3 ? null : AnalysisFormat.Finite(snapshot?.KendallTau),
                QuadraticWeightedKappa = AnalysisFormat.Finite(snapshot?.QuadraticWeightedKappa),
                MeanAbsoluteError = AnalysisFormat.Finite(snapshot?.MeanAbsoluteError),
                ExactMatchRate = AnalysisFormat.Finite(snapshot?.ExactMatchRate),
                Auc = AnalysisFormat.Finite(snapshot?.Auc),
                AucThreshold = AnalysisFormat.Finite(aucThreshold)
            };
        }

        /// <summary>
        /// The median human score of every snippet, used as the boundary between readable
        /// and unreadable for the AUC.
        /// </summary>
        ///
        /// <remarks>
        /// Taken over the whole dataset rather than per run, so that every run is measured
        /// against the same split regardless of which of its answers were unusable.
        /// </remarks>
        public static double ReadableThreshold(IEnumerable<JudgedRun> runs)
        {
            return AgreementMetrics.Median(runs
                .SelectMany(run => run.ConcreteAnswers)
                .DistinctBy(answer => answer.SnippetId)
                .Select(answer => answer.OriginalScore));
        }
    }
}
