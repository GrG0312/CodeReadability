using System.Globalization;

namespace CodeReadabilityLib.Analysis.Shared
{
    /// <summary>
    /// Number handling shared by the analyses: undefined values become missing values, and
    /// conclusions use invariant formatting.
    /// </summary>
    internal static class AnalysisFormat
    {
        /// <summary>
        /// <see langword="null"/> for a non-finite value, so that it serialises as a missing
        /// value rather than the literal <c>NaN</c>, which is not valid JSON.
        /// </summary>
        public static double? Finite(double value)
        {
            return double.IsFinite(value) ? value : null;
        }

        public static double? Finite(double? value)
        {
            return value is double v ? Finite(v) : null;
        }

        public static string Format(double value, string format = "0.000")
        {
            return double.IsFinite(value) ? value.ToString(format, CultureInfo.InvariantCulture) : "n/a";
        }

        public static string FormatP(double value)
        {
            return !double.IsFinite(value)
                ? "n/a"
                : value < 0.001 ? "< 0.001" : value.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
