namespace CodeReadabilityLib.Llms.Judging
{
    /// <summary>
    /// Why a model gave no usable answer. A failure is a result in its own right: it shows a
    /// limitation of the model, so it is recorded and counted, never retried away.
    /// </summary>
    public static class FailureKinds
    {
        /// <summary>
        /// The reply used up its assigned tokens (answer plus thinking budget) before it
        /// contained an answer.
        /// </summary>
        public const string CutOff = "cut-off";

        /// <summary>
        /// The reply finished but held no answer in the required format.
        /// </summary>
        public const string NoScore = "no-score";

        /// <summary>
        /// The reply held a score outside the rating scale.
        /// </summary>
        public const string OutOfRange = "out-of-range";

        /// <summary>
        /// The model server refused the request itself, for example because it did not fit
        /// the context. Unlike the others this points at the setup rather than the model.
        /// </summary>
        public const string Rejected = "rejected";

        /// <summary>
        /// A failure recorded before the kinds existed.
        /// </summary>
        public const string Unspecified = "unspecified";
    }
}
