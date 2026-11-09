using System.Globalization;
using System.Text.RegularExpressions;

namespace CodeReadabilityLib.Analysis.ModelAxes
{
    /// <summary>
    /// Orders the free-text size and quantization values of model descriptions.
    /// </summary>
    ///
    /// <remarks>
    /// Only used to decide which model is "B" in a comparison (the larger or higher-precision
    /// one). A value that cannot be read sorts first and the comparison is still made.
    /// </remarks>
    internal static partial class AxisOrdering
    {
        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*([bm])", RegexOptions.IgnoreCase)]
        private static partial Regex SizePattern();

        [GeneratedRegex(@"(\d+)")]
        private static partial Regex BitsPattern();

        /// <summary>
        /// The parameter count in billions, read from text such as <c>14B</c>, <c>35B-A3B</c>
        /// (the total count) or <c>500M</c>.
        /// </summary>
        public static double SizeInBillions(string size)
        {
            Match match = SizePattern().Match(size);
            if (!match.Success)
            {
                return 0;
            }

            double value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return match.Groups[2].Value.Equals("m", StringComparison.OrdinalIgnoreCase) ? value / 1000 : value;
        }

        /// <summary>
        /// The bits per weight, read from text such as <c>Q4_K_M</c>, <c>Q8_0</c> or
        /// <c>BF16</c>.
        /// </summary>
        public static int Bits(string quant)
        {
            Match match = BitsPattern().Match(quant);
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        }
    }
}
