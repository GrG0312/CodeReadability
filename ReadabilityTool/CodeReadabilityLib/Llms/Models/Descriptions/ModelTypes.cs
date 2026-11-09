namespace CodeReadabilityLib.Llms.Models.Descriptions
{
    /// <summary>
    /// The allowed values of a model description's <c>type</c>, and the placeholder that
    /// marks a value the user has not filled in yet.
    /// </summary>
    public static class ModelTypes
    {
        /// <summary>
        /// A model that thinks before it answers.
        /// </summary>
        public const string Reasoning = "reasoning";

        /// <summary>
        /// A model that answers directly.
        /// </summary>
        public const string Flash = "flash";

        /// <summary>
        /// One model file that can run with thinking on or off. Judged twice, once per mode.
        /// </summary>
        public const string Hybrid = "hybrid";

        /// <summary>
        /// Marks any description value the user has not filled in yet.
        /// </summary>
        public const string Placeholder = "not-designated";

        /// <summary>
        /// Whether <paramref name="type"/> is a known type or the placeholder, ignoring case
        /// and surrounding spaces.
        /// </summary>
        public static bool IsAllowed(string? type)
        {
            string normalized = Normalize(type);
            return normalized is Reasoning or Flash or Hybrid or Placeholder;
        }

        /// <summary>
        /// Lower-cases and trims a description value, the form in which values are compared.
        /// </summary>
        public static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
