namespace CodeReadabilityLib.Llms.Models.Descriptions
{
    /// <summary>
    /// The llama.cpp request fields a model description may set as recommended parameters.
    /// </summary>
    ///
    /// <remarks>
    /// Only sampling settings: the seed, the token limit and the reply format stay under the
    /// tool's control, so that every model runs the same experiment. An unknown name is
    /// refused rather than passed on, because the server would silently ignore a misspelt one.
    /// </remarks>
    public static class SamplingParameterNames
    {
        public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "temperature",
            "top_k",
            "top_p",
            "min_p",
            "typical_p",
            "repeat_penalty",
            "repeat_last_n",
            "presence_penalty",
            "frequency_penalty"
        };
    }
}
