namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// The outcome of a Spearman rank-correlation test.
    /// </summary>
    ///
    /// <remarks>
    /// Both <see cref="Rho"/> and <see cref="PValue"/> are <see cref="double.NaN"/> when the
    /// correlation is undefined, which happens whenever one of the two series has zero
    /// variance. A model that answers the same score for every snippet produces exactly
    /// that, and reporting it as a correlation of zero would wrongly suggest the model was
    /// measured and found uncorrelated rather than not measurable at all.
    /// </remarks>
    ///
    /// <param name="Rho">
    /// The correlation coefficient, in the range -1 to 1 inclusive.
    /// </param>
    ///
    /// <param name="PValue">
    /// The two-sided probability of observing a correlation at least this strong under the
    /// null hypothesis that the two series are unrelated.
    /// </param>
    ///
    /// <param name="SampleSize">
    /// The number of paired observations the test was computed from.
    /// </param>
    public sealed record SpearmanResult(double Rho, double PValue, int SampleSize)
    {
        /// <summary>
        /// Whether the correlation is statistically significant at the conventional
        /// 5% threshold. Undefined correlations are never significant.
        /// </summary>
        public bool IsSignificant => !double.IsNaN(PValue) && PValue < 0.05;

        /// <summary>
        /// Whether a correlation could be computed at all.
        /// </summary>
        public bool IsDefined => !double.IsNaN(Rho);
    }
}
