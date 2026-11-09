namespace CodeReadabilityLib.Statistics.Reliability
{
    /// <summary>
    /// Intraclass correlation: how far several raters agree on the same subjects.
    /// </summary>
    public static class IntraclassCorrelation
    {
        /// <summary>
        /// ICC(3,k): two-way mixed model, consistency, average of k raters.
        /// </summary>
        ///
        /// <remarks>
        /// Consistency rather than absolute agreement, because the averaged grades are only
        /// used through rank correlations, where a rater that is systematically stricter
        /// than another does no harm. "Average of k raters" because the analyses use the
        /// averaged grade, which is more reliable than any single rater's. Computed as
        /// (MS_rows − MS_error) / MS_rows (Shrout &amp; Fleiss, 1979).
        /// </remarks>
        ///
        /// <param name="ratings">
        /// One row per subject, one column per rater, with no missing values.
        /// </param>
        ///
        /// <returns>
        /// The ICC, or NaN with fewer than two subjects or two raters, or when the ratings do
        /// not vary at all.
        /// </returns>
        public static double ConsistencyOfAverage(IReadOnlyList<double[]> ratings)
        {
            int n = ratings.Count;
            int k = n == 0 ? 0 : ratings[0].Length;
            if (n < 2 || k < 2)
            {
                return double.NaN;
            }

            double grandMean = ratings.Sum(row => row.Sum()) / (n * k);

            double[] rowMeans = ratings.Select(row => row.Average()).ToArray();
            double[] columnMeans = Enumerable.Range(0, k).Select(j => ratings.Average(row => row[j])).ToArray();

            double rowsSquares = k * rowMeans.Sum(mean => (mean - grandMean) * (mean - grandMean));
            double columnsSquares = n * columnMeans.Sum(mean => (mean - grandMean) * (mean - grandMean));
            double totalSquares = ratings.Sum(row => row.Sum(value => (value - grandMean) * (value - grandMean)));
            double errorSquares = totalSquares - rowsSquares - columnsSquares;

            double meanSquareRows = rowsSquares / (n - 1);
            double meanSquareError = errorSquares / ((n - 1) * (k - 1));

            return meanSquareRows <= 0 ? double.NaN : (meanSquareRows - meanSquareError) / meanSquareRows;
        }

        /// <summary>
        /// The agreement band of an ICC value (Koo &amp; Li, 2016).
        /// </summary>
        public static string Band(double icc)
        {
            return !double.IsFinite(icc) ? "not computed"
                : icc < 0.5 ? "poor"
                : icc < 0.75 ? "moderate"
                : icc < 0.9 ? "good"
                : "excellent";
        }
    }
}
