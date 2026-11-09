namespace CodeReadabilityLib.Analysis.Shared
{
    /// <summary>
    /// A fixed set of bootstrap resamples of the snippets, drawn once from one seed.
    /// </summary>
    ///
    /// <remarks>
    /// Drawing every resample up front lets the expensive per-resample work run in parallel
    /// while the result stays exactly reproducible: resample <c>b</c> is always the same
    /// indices, whichever thread computes it.
    /// </remarks>
    internal sealed class SnippetBootstrap
    {
        /// <summary>
        /// One array of snippet indices per resample.
        /// </summary>
        public int[][] Resamples { get; }

        public int Iterations => Resamples.Length;

        public SnippetBootstrap(int snippets, int iterations, int seed)
        {
            Random random = new Random(seed);
            Resamples = new int[iterations][];

            for (int b = 0; b < iterations; b++)
            {
                int[] indices = new int[snippets];
                for (int i = 0; i < snippets; i++)
                {
                    indices[i] = random.Next(snippets);
                }

                Resamples[b] = indices;
            }
        }
    }
}
