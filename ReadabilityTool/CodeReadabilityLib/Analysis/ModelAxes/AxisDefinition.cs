using CodeReadabilityLib.Llms.Models.Descriptions;

namespace CodeReadabilityLib.Analysis.ModelAxes
{
    /// <summary>
    /// One characteristic models can differ in, and how to compare along it.
    /// </summary>
    internal sealed record AxisDefinition
    {
        /// <summary>
        /// The axis name: <c>quantization</c>, <c>size</c>, <c>reasoning</c> or <c>family</c>.
        /// </summary>
        public required string Name { get; init; }

        public required string Description { get; init; }

        /// <summary>
        /// The characteristic that varies along the axis, as written in the description.
        /// </summary>
        public required Func<ModelDescription, string> Value { get; init; }

        /// <summary>
        /// Orders the values so that B in every comparison is the "higher" one.
        /// </summary>
        public required Func<ModelDescription, IComparable> Order { get; init; }

        /// <summary>
        /// The characteristics held fixed, which must be equal for two models to be compared.
        /// </summary>
        public required Func<ModelDescription, string[]> Fixed { get; init; }

        /// <summary>
        /// What "B" and "A" are, for the summary, such as "the larger model".
        /// </summary>
        public required string HigherLabel { get; init; }

        public required string LowerLabel { get; init; }
    }
}
