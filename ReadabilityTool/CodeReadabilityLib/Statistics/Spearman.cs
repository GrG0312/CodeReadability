namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// Computes Spearman's rank correlation coefficient between two series.
    /// </summary>
    ///
    /// <remarks>
    /// This is the primary agreement measure for the project: it asks whether a model
    /// orders snippets the same way humans do, without requiring it to reproduce the
    /// exact numeric scores. Ties are resolved with mid-ranks, so repeated scores (which
    /// are common on a 1-5 scale) do not distort the result.
    /// </remarks>
    public static class Spearman
    {
        /// <summary>
        /// Computes the rank correlation between <paramref name="x"/> and <paramref name="y"/>.
        /// </summary>
        ///
        /// <param name="x">
        /// The first series, typically the human reference scores.
        /// </param>
        ///
        /// <param name="y">
        /// The second series, typically the model's predictions. Must be the same length
        /// as <paramref name="x"/> and aligned with it element by element.
        /// </param>
        ///
        /// <returns>
        /// A <see cref="SpearmanResult"/> holding the coefficient and its two-sided p-value.
        /// Both are <see cref="double.NaN"/> when either series has zero variance.
        /// </returns>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the series differ in length, or when fewer than three observations
        /// are supplied, which is the minimum for which a p-value is defined.
        /// </exception>
        public static SpearmanResult Compute(IReadOnlyList<double> x, IReadOnlyList<double> y)
        {
            if (x.Count != y.Count)
            {
                throw new ArgumentException("Both series must have the same number of observations.", nameof(y));
            }

            if (x.Count < 3)
            {
                throw new ArgumentException("At least 3 observations are required.", nameof(x));
            }

            double rho = Correlation.Spearman(x, y);
            int n = x.Count;

            // A NaN coefficient means one series was constant, so there is nothing to test.
            if (double.IsNaN(rho))
            {
                return new SpearmanResult(double.NaN, double.NaN, n);
            }

            return new SpearmanResult(rho, StudentT.CorrelationPValue(rho, n - 2), n);
        }
    }
}
