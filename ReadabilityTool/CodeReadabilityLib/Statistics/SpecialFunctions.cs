namespace CodeReadabilityLib.Statistics
{
    /// <summary>
    /// Special functions underlying the distribution tail probabilities used for p-values.
    /// </summary>
    ///
    /// <remarks>
    /// Implemented directly so that reporting p-values does not require a third-party
    /// statistics package. Accuracy is far beyond what is needed to decide significance.
    /// </remarks>
    internal static class SpecialFunctions
    {
        private const double Epsilon = 1e-14;
        private const double Tiny = 1e-300;
        private const int MaxIterations = 500;

        /// <summary>
        /// The natural logarithm of the gamma function, via the Lanczos approximation.
        /// </summary>
        ///
        /// <remarks>
        /// The logarithm is used rather than the gamma function itself because the factorial
        /// growth of gamma overflows <see langword="double"/> for even modest sample sizes.
        /// </remarks>
        public static double LogGamma(double x)
        {
            double[] coefficients =
            [
                 76.18009172947146, -86.50532032941677, 24.01409824083091,
                 -1.231739572450155, 0.1208650973866179e-2, -0.5395239384953e-5
            ];

            double y = x;
            double tmp = x + 5.5;
            tmp -= (x + 0.5) * Math.Log(tmp);

            double series = 1.000000000190015;
            for (int i = 0; i < coefficients.Length; i++)
            {
                series += coefficients[i] / ++y;
            }

            return -tmp + Math.Log(2.5066282746310005 * series / x);
        }

        /// <summary>
        /// The regularised incomplete beta function, commonly written I_x(a, b).
        /// </summary>
        public static double RegularizedIncompleteBeta(double x, double a, double b)
        {
            if (x <= 0) return 0.0;
            if (x >= 1) return 1.0;

            double front = Math.Exp(
                LogGamma(a + b) - LogGamma(a) - LogGamma(b) +
                a * Math.Log(x) + b * Math.Log(1 - x));

            // The continued fraction converges quickly only on one side of this pivot,
            // so reflect to the mirrored form when x falls on the slow side.
            return x < (a + 1) / (a + b + 2)
                ? front * BetaContinuedFraction(x, a, b) / a
                : 1 - front * BetaContinuedFraction(1 - x, b, a) / b;
        }

        /// <summary>
        /// The upper regularised incomplete gamma function Q(a, x) = Γ(a, x) / Γ(a).
        /// </summary>
        public static double UpperRegularizedGamma(double a, double x)
        {
            if (x <= 0) return 1.0;

            // The series converges fast below a + 1 and the continued fraction above it.
            return x < a + 1
                ? 1.0 - LowerGammaSeries(a, x)
                : UpperGammaContinuedFraction(a, x);
        }

        /// <summary>
        /// The complementary error function.
        /// </summary>
        public static double Erfc(double x)
        {
            // erfc(x) = Q(1/2, x^2) for non-negative x, and reflects for negative x.
            double value = UpperRegularizedGamma(0.5, x * x);
            return x >= 0 ? value : 2.0 - value;
        }

        /// <summary>
        /// Evaluates the continued fraction expansion of the incomplete beta function using
        /// the modified Lentz algorithm.
        /// </summary>
        private static double BetaContinuedFraction(double x, double a, double b)
        {
            double f = 1;
            double c = 1;
            double d = 0;

            for (int i = 0; i <= MaxIterations; i++)
            {
                int m = i / 2;

                // The expansion alternates between two different numerator forms.
                double numerator;
                if (i == 0)
                {
                    numerator = 1.0;
                }
                else if (i % 2 == 0)
                {
                    numerator = m * (b - m) * x / ((a + 2 * m - 1) * (a + 2 * m));
                }
                else
                {
                    numerator = -(a + m) * (a + b + m) * x / ((a + 2 * m) * (a + 2 * m + 1));
                }

                d = 1 + numerator * d;
                if (Math.Abs(d) < Tiny) d = Tiny;
                d = 1 / d;

                c = 1 + numerator / c;
                if (Math.Abs(c) < Tiny) c = Tiny;

                double delta = c * d;
                f *= delta;

                if (Math.Abs(1 - delta) < Epsilon)
                {
                    break;
                }
            }

            return f - 1;
        }

        /// <summary>
        /// The lower regularised incomplete gamma function P(a, x) by its power series.
        /// </summary>
        private static double LowerGammaSeries(double a, double x)
        {
            double term = 1.0 / a;
            double sum = term;

            for (int n = 1; n <= MaxIterations; n++)
            {
                term *= x / (a + n);
                sum += term;

                if (Math.Abs(term) < Math.Abs(sum) * Epsilon)
                {
                    break;
                }
            }

            return sum * Math.Exp(-x + a * Math.Log(x) - LogGamma(a));
        }

        /// <summary>
        /// Q(a, x) by its continued fraction, using the modified Lentz algorithm.
        /// </summary>
        private static double UpperGammaContinuedFraction(double a, double x)
        {
            double b = x + 1 - a;
            double c = 1 / Tiny;
            double d = 1 / b;
            double h = d;

            for (int i = 1; i <= MaxIterations; i++)
            {
                double an = -i * (i - a);
                b += 2;

                d = an * d + b;
                if (Math.Abs(d) < Tiny) d = Tiny;

                c = b + an / c;
                if (Math.Abs(c) < Tiny) c = Tiny;

                d = 1 / d;
                double delta = d * c;
                h *= delta;

                if (Math.Abs(delta - 1) < Epsilon)
                {
                    break;
                }
            }

            return Math.Exp(-x + a * Math.Log(x) - LogGamma(a)) * h;
        }
    }
}
