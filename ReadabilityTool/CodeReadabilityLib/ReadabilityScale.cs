namespace CodeReadabilityLib
{
    /// <summary>
    /// The fixed readability score scale used throughout the tool.
    /// </summary>
    ///
    /// <remarks>
    /// Applies to readability scores only: the human reference scores and the models'
    /// readability judgements. Metrics keep their own ranges. The scale is deliberately not
    /// configurable, so that every result is measured on the same scale and output files
    /// need not record it.
    /// </remarks>
    public static class ReadabilityScale
    {
        /// <summary>
        /// The lowest readability score, meaning very poor readability.
        /// </summary>
        public const int Min = 1;

        /// <summary>
        /// The highest readability score, meaning excellent readability.
        /// </summary>
        public const int Max = 5;
    }
}
