using CodeReadabilityLib.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CodeReadabilityLib.Llms.ServerHosting
{
    /// <summary>
    /// Represents a host for a Llama server, providing functionality to manage the server's lifecycle and resources.
    /// </summary>
    public sealed class LlamaServerHost : IAsyncDisposable
    {
        /// <summary>
        /// The process associated with the Llama server.
        /// </summary>
        private readonly Process _process;

        /// <summary>
        /// The stream writer used for logging server output, if any.
        /// </summary>
        private readonly StreamWriter? _logWriter;

        /// <summary>
        /// The logger used for logging server events and information.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Indicates whether the host has been disposed.
        /// </summary>
        private bool _disposed;


        /// <summary>
        /// Gets the name of the model being served by the Llama server.
        /// </summary>
        public string ModelName { get; }

        /// <summary>
        /// Gets the base address of the Llama server.
        /// </summary>
        public Uri BaseAddress { get; }

        /// <summary>
        /// Gets the number of parallel slots available for processing requests on the Llama server.
        /// </summary>
        public ushort ParallelSlots { get; }

        private LlamaServerHost(Process process, StreamWriter? logWriter, ILogger logger, string modelName, Uri baseAddress, ushort parallelSlots)
        {
            _process = process;
            _logWriter = logWriter;
            _logger = logger;
            ModelName = modelName;
            BaseAddress = baseAddress;
            ParallelSlots = parallelSlots;
        }

        /// <summary>
        /// Starts a new instance of the Llama server host with the specified options and logger.
        /// </summary>
        /// 
        /// <param name="options">
        /// The options used to configure the Llama server host.
        /// </param>
        /// 
        /// <param name="logger">
        /// The logger used to log server events and information.
        /// </param>
        /// 
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the server startup process.
        /// </param>
        /// 
        /// <returns>
        /// A task that represents the asynchronous operation of starting the Llama server host.
        /// The task result contains the started LlamaServerHost instance.
        /// </returns>
        /// 
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task<LlamaServerHost> StartAsync(LlamaServerOptions options, ILogger logger, CancellationToken cancellationToken = default)
        {
            // First check if the model file exists at the specified path
            if (!File.Exists(options.ModelPath))
            {
                throw new FileNotFoundException($"Model file not found at path: {options.ModelPath}", options.ModelPath);
            }

            // Determine the port and model name based on the provided options
            int port = options.Port > 0 ? (int)options.Port : FindFreePort();
            string modelName = Path.GetFileNameWithoutExtension(options.ModelPath);

            // Construct the command-line arguments for starting the Llama server process
            // Note: host 127.0.0.1 is used to bind the server to localhost for security reasons
            // --jinja makes the server use the model's own chat template, which is what
            // receives per-request template values such as a hybrid model's thinking switch.
            string arguments =
                $"-m \"{options.ModelPath}\" --host 127.0.0.1 --port {port} --jinja " +
                $"-ngl {options.GpuLayers} --ctx-size {options.ContextSize} " +
                $"--parallel {options.ParallelSlots} {options.ExtraArguments}".TrimEnd();

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = options.ExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set the CUDA_VISIBLE_DEVICES environment variable if it is set in the current environment
            // The last part of the check means: ... is a string with a length greater than 0, and assign it to the variable 'cuda'
            if (Environment.GetEnvironmentVariable("CUDA_VISIBLE_DEVICES") is { Length: > 0 } cuda)
            {
                processStartInfo.Environment["CUDA_VISIBLE_DEVICES"] = cuda;
            }

            // The log is opened before the server starts, so that a log problem cannot leave
            // a started server behind. Relative to the working folder, not the program's:
            // inside the cluster's container the program's folder is read-only.
            string logDirectory = Path.GetFullPath(options.LogDirectory ?? Path.Combine("logs", "llama"));
            string logPath = Path.Combine(logDirectory, $"{modelName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            StreamWriter logWriter;
            try
            {
                Directory.CreateDirectory(logDirectory);
                logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Cannot write the llama-server log '{logPath}': {ex.Message}", ex);
            }

            logger.Info($"Starting llama-server for '{modelName}' on port {port}...");

            // Start the Llama server process and handle any exceptions that may occur during startup
            Process? process;
            try
            {
                process = Process.Start(processStartInfo);

                if (process is null)
                {
                    throw new InvalidOperationException("Process.Start returned null. Failed to start Llama-server.");
                }
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                logWriter.Dispose();
                throw new InvalidOperationException(
                    $"Failed to launch '{options.ExecutablePath}'. Ensure llama-server is installed and on PATH, " +
                            "or pass --llama-server-path.", ex);
            }

            try
            {
                logger.Debug($"Logging llama-server output to: {logPath}");

                // Start reading the output and error streams asynchronously
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (e.Data is not null)
                    {
                        logWriter.WriteLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    if (e.Data is not null)
                    {
                        logWriter.WriteLine($"ERR: {e.Data}");
                    }
                };

                // Start reading the output and error streams asynchronously
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait until the server is healthy and ready to accept requests
                Uri baseAddress = new Uri($"http://127.0.0.1:{port}/");
                await WaitUntilHealthyAsync(baseAddress, process, options.StartupTimeout, logger, cancellationToken).ConfigureAwait(false);

                logger.Info($"Llama-server for '{modelName}' is ready at {baseAddress}.");

                return new LlamaServerHost(process, logWriter, logger, modelName, baseAddress, options.ParallelSlots);
            }
            catch
            {
                // If an exception occurs during startup, ensure the process is terminated and resources are cleaned up
                KillQuietly(process);
                logWriter.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Waits until the Llama server is healthy and ready to accept requests by probing its health endpoint.
        /// </summary>
        /// 
        /// <param name="baseAddress">
        /// The base address of the Llama server to probe for health status.
        /// </param>
        /// 
        /// <param name="process">
        /// The process associated with the Llama server, used to check if it has exited unexpectedly.
        /// </param>
        /// 
        /// <param name="startupTimeout">
        /// The maximum duration to wait for the server to become healthy before timing out.
        /// </param>
        /// 
        /// <param name="logger">
        /// The logger used to log server events and information during the health check process.
        /// </param>
        /// 
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the health check process.
        /// </param>
        /// 
        /// <returns>
        /// A task that represents the asynchronous operation of waiting for the server to become healthy.
        /// </returns>
        /// 
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="TimeoutException"></exception>
        private static async Task WaitUntilHealthyAsync(Uri baseAddress, Process process, TimeSpan startupTimeout, ILogger logger, CancellationToken cancellationToken)
        {
            // Use HttpClient to probe the health endpoint of the Llama server
            using HttpClient probe = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(5) };
            // Calculate the deadline for the startup timeout
            DateTime deadline = DateTime.UtcNow + startupTimeout;

            // Continuously probe the health endpoint until the server is ready or the timeout is reached
            while (DateTime.UtcNow < deadline)
            {
                // Check for cancellation requests and throw if requested
                cancellationToken.ThrowIfCancellationRequested();

                // Fail fast if the server died rather than waiting out the full timeout
                if (process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"llama-server exited with code {process.ExitCode} before becoming ready. Check the log file.");
                }

                try
                {
                    // Probe the health endpoint this iteration to check if the server is ready
                    using HttpResponseMessage response = await probe.GetAsync("health", cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode) return;
                }
                catch (HttpRequestException) { /* not listening yet */ }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { /* probe timeout */ }

                // Wait for a second before the next probe to avoid overwhelming the server with requests
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException($"llama-server did not become ready within {startupTimeout}.");
        }

        /// <summary>
        /// Finds a free TCP port on the local machine by binding a socket to port 0, which allows the operating system to assign an available port.
        /// </summary>
        /// 
        /// <returns>
        /// The free TCP port number that was assigned by the operating system.
        /// </returns>
        private static int FindFreePort()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            // QUESTION: if the socket is bound to the port, can the port still be used by another process?
            // QUESTION: if not, is there a guarantee that the port will be free when the llama-server process starts?
            return ((IPEndPoint)socket.LocalEndPoint!).Port; 
        }

        /// <summary>
        /// Attempts to kill the specified process and its entire process tree, ignoring any exceptions that may occur during the operation.
        /// </summary>
        private static void KillQuietly(Process process)
        {
            // Attempt to kill the process and its entire process tree, ignoring any exceptions that may occur
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }

            // Ignore exceptions that may occur when trying to kill the process
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }

            // Ensure the process is disposed of to free up resources, regardless of whether the kill operation succeeded or failed
            finally { process.Dispose(); }
        }

        /// <summary>
        /// Disposes of the Llama server host asynchronously, stopping the associated process and releasing any resources used by the host.
        /// </summary>
        /// 
        /// <returns>
        /// A task that represents the asynchronous operation of disposing the Llama server host.
        /// </returns>
        public async ValueTask DisposeAsync()
        {
            // If the host has already been disposed, return immediately to avoid double disposal
            if (_disposed) return;

            // Mark the host as disposed to prevent further disposal attempts
            _disposed = true;
            _logger.Debug($"Stopping llama-server ({ModelName})...");
            try
            {
                // Attempt to kill the process and its entire process tree if it has not already exited
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                }
            }

            // Catch any exceptions that may occur during the process termination and log a warning message
            catch (Exception ex) { _logger.Warning($"Error stopping llama-server: {ex.Message}"); }

            // Ensure that the process and log writer are disposed of to free up resources, regardless of whether the termination succeeded or failed
            finally
            {
                _process.Dispose();
                _logWriter?.Dispose();
            }
        }
    }
}
