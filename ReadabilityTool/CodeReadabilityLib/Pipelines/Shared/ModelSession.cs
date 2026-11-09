using CodeReadabilityLib.Checkpointing;
using CodeReadabilityLib.Llms.ServerHosting;
using CodeReadabilityLib.Logging;

namespace CodeReadabilityLib.Pipelines.Shared
{
    /// <summary>
    /// Runs one model's work on a model server, restarting the server if it crashes or stops
    /// responding.
    /// </summary>
    ///
    /// <remarks>
    /// The work must be safe to run again: it skips what the checkpoint store already holds.
    /// After <see cref="MaxRestarts"/> failed restarts the execution stops in a resumable
    /// state rather than recording the failures as answers.
    /// </remarks>
    public static class ModelSession
    {
        /// <summary>
        /// Restarts allowed for one model before giving up.
        /// </summary>
        public const int MaxRestarts = 3;

        /// <summary>
        /// The pause before the first restart; each further restart waits this much longer.
        /// </summary>
        private static readonly TimeSpan RestartPause = TimeSpan.FromSeconds(10);

        /// <param name="work">
        /// Receives the running client. Called again after every restart.
        /// </param>
        ///
        /// <returns>
        /// The llama.cpp build the server reported, or <see langword="null"/>.
        /// </returns>
        ///
        /// <exception cref="ResumableStopException">
        /// Thrown when the server kept failing after <see cref="MaxRestarts"/> restarts.
        /// </exception>
        public static async Task<string?> RunAsync(
            LlamaServerOptions options,
            CheckpointStore store,
            ILogger logger,
            Func<LlamaCppClient, Task> work,
            CancellationToken cancellationToken)
        {
            string modelFile = Path.GetFileNameWithoutExtension(options.ModelPath);
            string originalPath = options.ModelPath;
            string loadPath = ModelStaging.Stage(originalPath, logger);
            options = options with { ModelPath = loadPath };

            try
            {
                return await RunWithRestartsAsync(options, store, logger, work, modelFile, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ModelStaging.Release(loadPath, originalPath);
            }
        }

        private static async Task<string?> RunWithRestartsAsync(
            LlamaServerOptions options,
            CheckpointStore store,
            ILogger logger,
            Func<LlamaCppClient, Task> work,
            string modelFile,
            CancellationToken cancellationToken)
        {
            for (int restarts = 0; ; restarts++)
            {
                try
                {
                    await using LlamaCppClient client = await LlamaCppClient.StartAsync(options, logger, cancellationToken).ConfigureAwait(false);

                    string? version = await client.GetServerVersionAsync(cancellationToken).ConfigureAwait(false);
                    store.EnsureLlamaCppVersion(version);

                    await work(client).ConfigureAwait(false);
                    return version;
                }
                catch (ModelServerException ex) when (restarts < MaxRestarts)
                {
                    TimeSpan pause = RestartPause * (restarts + 1);
                    logger.Warning($"{modelFile}: {ex.Message} Restarting the server in {pause.TotalSeconds:0} s ({restarts + 1}/{MaxRestarts}).");
                    await Task.Delay(pause, cancellationToken).ConfigureAwait(false);
                }
                catch (ModelServerException ex)
                {
                    throw new ResumableStopException(
                        $"{modelFile}: the model server kept failing after {MaxRestarts} restarts ({ex.Message}).",
                        store.Folder,
                        ex);
                }
            }
        }
    }
}
