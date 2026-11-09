using System.Text.Json.Serialization;

namespace CodeReadabilityLib.Analysis.Shared
{
    /// <summary>
    /// A confidence interval as written to the statistics file.
    /// </summary>
    public sealed record Interval
    {
        [JsonPropertyName("lower")]
        public required double Lower { get; init; }

        [JsonPropertyName("upper")]
        public required double Upper { get; init; }
    }
}
