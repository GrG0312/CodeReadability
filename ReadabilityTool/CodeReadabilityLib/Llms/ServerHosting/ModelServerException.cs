namespace CodeReadabilityLib.Llms.ServerHosting
{
    /// <summary>
    /// An infrastructure failure: the model server crashed, could not be reached, or did not
    /// respond in time. The model never really answered.
    /// </summary>
    ///
    /// <remarks>
    /// Kept apart from a model failure (the model replied, but unusably), because an
    /// infrastructure failure must never be recorded as an answer: the request is repeated
    /// after the server is restarted, or when the execution is resumed.
    /// </remarks>
    public sealed class ModelServerException : Exception
    {
        public ModelServerException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
