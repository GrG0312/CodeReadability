using CodeReadabilityLib.Llms.ServerHosting;

namespace ReadabilityConsoleApp.Cli
{
    /// <summary>
    /// Reads the model server options every model-running command shares.
    /// </summary>
    public static class ServerOptionsReader
    {
        /// <summary>
        /// The server options given on the command line, with an empty model path that the
        /// pipelines fill in per model.
        /// </summary>
        public static LlamaServerOptions Read(CommandLine args)
        {
            return new LlamaServerOptions
            {
                ModelPath = string.Empty,
                ExecutablePath = args.Optional("llama-server-path") ?? "llama-server",
                Port = (uint)args.Int("port", 0),
                GpuLayers = (byte)args.Int("gpu-layers", 99),
                ContextSize = (uint)args.Int("ctx-size", 4096),
                LogDirectory = args.Optional("log-dir"),
                ExtraArguments = args.Optional("server-args") ?? string.Empty
            };
        }
    }
}
