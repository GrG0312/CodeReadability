namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// Every agreement statistic between a model and the human reference, computed together.
    /// </summary>
    ///
    /// <remarks>
    /// Computing them in one place guarantees that the benchmark, the <c>report</c> command
    /// and every bootstrap resample define each statistic identically.
    /// </remarks>
    public readonly record struct AgreementSnapshot(
        double Spearman,
        double Pearson,
        double KendallTau,
        double QuadraticWeightedKappa,
        double MeanAbsoluteError,
        double ExactMatchRate,
        double Auc)
    {
        /// <summary>
        /// The number of statistics, in the order of <see cref="ToArray"/>.
        /// </summary>
        public const int Count = 7;

        /// <summary>
        /// Computes every statistic for aligned reference and predicted scores.
        /// </summary>
        ///
        /// <param name="aucThreshold">
        /// Reference scores above this value count as readable for the AUC.
        /// </param>
        public static AgreementSnapshot Compute(
            IReadOnlyList<double> reference,
            IReadOnlyList<double> predicted,
            int minScore,
            int maxScore,
            double aucThreshold)
        {
            if (reference.Count == 0)
            {
                return new AgreementSnapshot(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            }

            int exact = 0;
            int[] roundedReference = new int[reference.Count];
            int[] roundedPredicted = new int[reference.Count];

            for (int i = 0; i < reference.Count; i++)
            {
                // Reference scores are usually means of several raters (3.89, 4.22, ...), so
                // a match is an answer less than one point away. This is the definition the
                // distance buckets have always used, and existing reports depend on it.
                if (Math.Abs(reference[i] - predicted[i]) < 1)
                {
                    exact++;
                }

                roundedReference[i] = (int)Math.Round(reference[i]);
                roundedPredicted[i] = (int)Math.Round(predicted[i]);
            }

            return new AgreementSnapshot(
                Spearman: Correlation.Spearman(reference, predicted),
                Pearson: Correlation.Pearson(reference, predicted),
                KendallTau: Correlation.KendallTauB(reference, predicted),
                QuadraticWeightedKappa: AgreementMetrics.QuadraticWeightedKappa(roundedReference, roundedPredicted, minScore, maxScore),
                MeanAbsoluteError: AgreementMetrics.MeanAbsoluteError(reference, predicted),
                ExactMatchRate: exact * 100.0 / reference.Count,
                Auc: double.IsNaN(aucThreshold) ? double.NaN : AgreementMetrics.Auc(reference, predicted, aucThreshold));
        }

        public double[] ToArray()
        {
            return [Spearman, Pearson, KendallTau, QuadraticWeightedKappa, MeanAbsoluteError, ExactMatchRate, Auc];
        }
    }
}
