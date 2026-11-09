namespace CodeReadabilityLib.Sources
{
    /// <summary>
    /// Represents a source of code samples for testing and demonstration purposes.
    /// </summary>
    public interface ICodeSampleSource
    {
        /// <summary>
        /// Gets the name of the code sample source.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets an asynchronous enumerable of code samples from the source.
        /// </summary>
        /// 
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// 
        /// <returns>
        /// An asynchronous enumerable of <see cref="CodeSample"/> instances representing the code samples from the source.
        /// </returns>
        public IAsyncEnumerable<CodeSample> GetSamplesAsync(CancellationToken cancellationToken = default);
    }
}
