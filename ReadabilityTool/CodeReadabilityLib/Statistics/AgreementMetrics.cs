namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// Agreement measures for ordinal ratings, used to compare a model's scores against
    /// human reference scores.
    /// </summary>
    public static class AgreementMetrics
    {
        /// <summary>
        /// Computes the quadratic weighted kappa between reference and predicted ratings.
        /// </summary>
        ///
        /// <remarks>
        /// Kappa improves on a raw exact-match rate in two ways that matter here. It weights
        /// disagreements by how far apart they are, so being wrong by three counts far more
        /// than being wrong by one, and it corrects for the agreement that would arise by
        /// chance, so a model that always answers the most common score scores near zero
        /// instead of appearing accurate.
        /// </remarks>
        ///
        /// <param name="reference">
        /// The human reference ratings.
        /// </param>
        ///
        /// <param name="predicted">
        /// The model's ratings, aligned element by element with <paramref name="reference"/>.
        /// </param>
        ///
        /// <param name="minScore">
        /// The lowest rating on the scale, inclusive.
        /// </param>
        ///
        /// <param name="maxScore">
        /// The highest rating on the scale, inclusive.
        /// </param>
        ///
        /// <returns>
        /// A value of 1 for perfect agreement, 0 for chance-level agreement, and negative
        /// values for agreement worse than chance. Returns <see cref="double.NaN"/> when the
        /// inputs are empty or the scale has fewer than two categories.
        /// </returns>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the two series differ in length.
        /// </exception>
        public static double QuadraticWeightedKappa(
            IReadOnlyList<int> reference,
            IReadOnlyList<int> predicted,
            int minScore,
            int maxScore)
        {
            if (reference.Count != predicted.Count)
            {
                throw new ArgumentException("Both series must have the same number of observations.", nameof(predicted));
            }

            if (reference.Count == 0)
            {
                return double.NaN;
            }

            int categories = maxScore - minScore + 1;
            if (categories < 2)
            {
                return double.NaN;
            }

            // observed[i, j] counts how often the humans said i and the model said j.
            double[,] observed = new double[categories, categories];
            double[] referenceHistogram = new double[categories];
            double[] predictedHistogram = new double[categories];

            for (int i = 0; i < reference.Count; i++)
            {
                int r = Math.Clamp(reference[i], minScore, maxScore) - minScore;
                int p = Math.Clamp(predicted[i], minScore, maxScore) - minScore;

                observed[r, p]++;
                referenceHistogram[r]++;
                predictedHistogram[p]++;
            }

            double total = reference.Count;
            double weightedObserved = 0;
            double weightedExpected = 0;

            for (int i = 0; i < categories; i++)
            {
                for (int j = 0; j < categories; j++)
                {
                    // Quadratic weights, normalised so that the worst possible
                    // disagreement on this scale carries a weight of exactly 1.
                    double weight = Math.Pow(i - j, 2) / Math.Pow(categories - 1, 2);

                    // The agreement expected if the two raters were independent.
                    double expected = referenceHistogram[i] * predictedHistogram[j] / total;

                    weightedObserved += weight * observed[i, j];
                    weightedExpected += weight * expected;
                }
            }

            // A zero denominator means at least one rater used a single category, in which
            // case there is no disagreement to measure and agreement is trivially perfect.
            return weightedExpected == 0 ? 1.0 : 1.0 - weightedObserved / weightedExpected;
        }

        /// <summary>
        /// Computes the mean absolute error between reference and predicted ratings.
        /// </summary>
        ///
        /// <returns>
        /// The average absolute difference, or <see cref="double.NaN"/> when the inputs are empty.
        /// </returns>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the two series differ in length.
        /// </exception>
        public static double MeanAbsoluteError(IReadOnlyList<double> reference, IReadOnlyList<double> predicted)
        {
            if (reference.Count != predicted.Count)
            {
                throw new ArgumentException("Both series must have the same number of observations.", nameof(predicted));
            }

            if (reference.Count == 0)
            {
                return double.NaN;
            }

            double total = 0;
            for (int i = 0; i < reference.Count; i++)
            {
                total += Math.Abs(reference[i] - predicted[i]);
            }

            return total / reference.Count;
        }

        /// <summary>
        /// The area under the ROC curve for separating readable from unreadable snippets,
        /// where a snippet counts as readable when its reference score exceeds
        /// <paramref name="threshold"/>.
        /// </summary>
        ///
        /// <remarks>
        /// Earlier readability studies (Buse and Weimer, Scalabrino et al.) framed the task as
        /// binary classification and reported AUC, so this is needed to compare against them.
        /// Computed through the Mann-Whitney U statistic, which counts a tied prediction as
        /// half a correct ordering; that matters because a 1-5 judge ties constantly.
        /// </remarks>
        ///
        /// <returns>
        /// The AUC, or <see cref="double.NaN"/> when either class is empty.
        /// </returns>
        public static double Auc(IReadOnlyList<double> reference, IReadOnlyList<double> predicted, double threshold)
        {
            if (reference.Count != predicted.Count)
            {
                throw new ArgumentException("Both series must have the same number of observations.", nameof(predicted));
            }

            double[] ranks = Correlation.Rank(predicted);

            long positives = 0;
            double positiveRankSum = 0;

            for (int i = 0; i < reference.Count; i++)
            {
                if (reference[i] > threshold)
                {
                    positives++;
                    positiveRankSum += ranks[i];
                }
            }

            long negatives = reference.Count - positives;
            if (positives == 0 || negatives == 0)
            {
                return double.NaN;
            }

            double u = positiveRankSum - positives * (positives + 1) / 2.0;
            return u / ((double)positives * negatives);
        }

        /// <summary>
        /// The median of <paramref name="values"/>, or <see cref="double.NaN"/> when empty.
        /// </summary>
        public static double Median(IEnumerable<double> values)
        {
            double[] sorted = values.OrderBy(v => v).ToArray();
            if (sorted.Length == 0)
            {
                return double.NaN;
            }

            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2.0;
        }
    }
}
