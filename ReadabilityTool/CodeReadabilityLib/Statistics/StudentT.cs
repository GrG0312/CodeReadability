namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// Tail probabilities of the Student t-distribution.
    /// </summary>
    internal static class StudentT
    {
        /// <summary>
        /// The two-sided p-value for a t statistic.
        /// </summary>
        ///
        /// <param name="t">
        /// The absolute value of the t statistic.
        /// </param>
        ///
        /// <param name="degreesOfFreedom">
        /// The degrees of freedom, which for a correlation test is the sample size minus two.
        /// </param>
        ///
        /// <returns>
        /// The probability of observing a statistic at least as extreme as
        /// <paramref name="t"/> in either direction, or 1 when
        /// <paramref name="degreesOfFreedom"/> is not positive.
        /// </returns>
        public static double TwoSidedPValue(double t, int degreesOfFreedom)
        {
            if (degreesOfFreedom <= 0)
            {
                return 1.0;
            }

            double v = degreesOfFreedom;

            // For the Student t-distribution the two-sided tail area is exactly
            // I_{v/(v+t^2)}(v/2, 1/2), so no further transformation is needed.
            return SpecialFunctions.RegularizedIncompleteBeta(v / (v + t * t), v / 2.0, 0.5);
        }

        /// <summary>
        /// The two-sided p-value for testing whether a correlation coefficient differs from
        /// zero, using the t approximation with <paramref name="degreesOfFreedom"/>.
        /// </summary>
        ///
        /// <returns>
        /// The p-value, 0 for a perfect correlation, or <see cref="double.NaN"/> when
        /// <paramref name="r"/> is undefined.
        /// </returns>
        public static double CorrelationPValue(double r, int degreesOfFreedom)
        {
            if (double.IsNaN(r))
            {
                return double.NaN;
            }

            // A perfect correlation drives the t statistic to infinity.
            if (Math.Abs(r) >= 1.0)
            {
                return 0.0;
            }

            double t = r * Math.Sqrt(degreesOfFreedom / (1 - r * r));
            return TwoSidedPValue(Math.Abs(t), degreesOfFreedom);
        }
    }
}
