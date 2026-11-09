namespace CodeReadabilityLib.Llms.ServerHosting
{
    /// <summary>
    /// Represents configuration options for hosting a Llama server.
    /// </summary>
    public sealed record LlamaServerOptions
    {
        /// <summary>
        /// Gets or sets the path to the model file used by the Llama server.
        /// </summary>
        public required string ModelPath { get; init; }

        /// <summary>
        /// Gets or sets the path to the executable for the Llama server.
        /// </summary>
        public string ExecutablePath { get; init; } = "llama-server";

        /// <summary>
        /// Gets or sets the port number on which the Llama server will listen for incoming connections.
        /// Use 0 to auto-select a free port (recommended when running models back-to-back).
        /// </summary>
        public uint Port { get; init; }

        /// <summary>
        /// Gets or sets the number of GPU layers to use for inference.
        /// The default value is 99, which means that all layers will be processed on the GPU.
        /// </summary>
        public byte GpuLayers { get; init; } = 99;

        /// <summary>
        /// Gets or sets the context size for the Llama server.
        /// </summary>
        public uint ContextSize { get; init; } = 4096;

        /// <summary>
        /// Gets or sets the number of parallel slots for processing requests in the Llama server.
        /// </summary>
        public ushort ParallelSlots { get; init; } = 1;

        /// <summary>
        /// Gets or sets the directory where log files will be stored.
        /// </summary>
        public string? LogDirectory { get; init; }

        /// <summary>
        /// Gets or sets the timeout duration for the Llama server to start up.
        /// </summary>
        ///
        /// <remarks>
        /// Generous because a large model read from network storage, plus the GPU code the
        /// driver compiles on first use, can take many minutes. A server that crashes is
        /// noticed at once, so the limit only matters for one that hangs.
        /// </remarks>
        public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets any extra command-line arguments to pass to the Llama server executable.
        /// </summary>
        public string ExtraArguments { get; init; } = string.Empty;
    }
}
