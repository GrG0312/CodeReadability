using CodeReadabilityLib.Checkpointing;

namespace ReadabilityConsoleApp.Cli
{
    /// <summary>
    /// A parsed command line: the verb the user asked for, plus its options.
    /// </summary>
    ///
    /// <remarks>
    /// Hand-rolled rather than built on a parsing library, so that the option surface stays
    /// visible in one place and the tool keeps working across library major versions.
    /// </remarks>
    public sealed class CommandLine
    {
        private readonly Dictionary<string, string> _options;

        /// <summary>
        /// The verb, lower-cased. Defaults to <c>help</c> when none was supplied.
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// Whether debug-level logging was requested via <c>--verbose</c>.
        /// </summary>
        public bool Verbose => _options.ContainsKey("verbose");

        /// <summary>
        /// Every option as given, for saving into an execution plan.
        /// </summary>
        public IReadOnlyDictionary<string, string> Options => _options;

        /// <summary>
        /// The checkpoint store of the execution being resumed, or <see langword="null"/>
        /// for a new execution.
        /// </summary>
        public CheckpointStore? Resume { get; private init; }

        /// <summary>
        /// Words after the command that are not options, such as the command <c>submit</c>
        /// is asked to submit.
        /// </summary>
        public IReadOnlyList<string> Positionals { get; private init; } = [];

        private CommandLine(string command, Dictionary<string, string> options)
        {
            Command = command;
            _options = options;
        }

        /// <summary>
        /// The command line saved in an execution's plan, set up to resume that execution.
        /// </summary>
        /// <param name="jobOptions">
        /// Options of this job only, such as <c>only-model</c>, added to the saved ones.
        /// </param>
        public static CommandLine ForResume(CheckpointStore store, IReadOnlyDictionary<string, string>? jobOptions = null)
        {
            Dictionary<string, string> options = new Dictionary<string, string>(store.Plan.Options, StringComparer.OrdinalIgnoreCase);
            foreach ((string key, string value) in jobOptions ?? new Dictionary<string, string>())
            {
                options[key] = value;
            }

            return new CommandLine(store.Plan.Command, options)
            {
                Resume = store
            };
        }

        /// <summary>
        /// Parses raw process arguments of the form
        /// <c>&lt;command&gt; --key value --key=value --flag</c>.
        /// </summary>
        ///
        /// <remarks>
        /// An option with no following value, or followed by another option, is recorded as
        /// the string <c>"true"</c> so that it reads as a flag.
        /// </remarks>
        public static CommandLine Parse(string[] args)
        {
            Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            bool hasVerb = args.Length > 0 && !args[0].StartsWith('-');
            string command = hasVerb ? args[0].ToLowerInvariant() : "help";

            List<string> positionals = new List<string>();

            for (int i = hasVerb ? 1 : 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith('-'))
                {
                    positionals.Add(args[i]);
                    continue;
                }

                string key = args[i].TrimStart('-');

                // --key=value carries values that themselves start with "--", such as
                // --server-args="--jinja".
                int equals = key.IndexOf('=');
                if (equals > 0)
                {
                    options[key[..equals]] = key[(equals + 1)..];
                    continue;
                }

                bool hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--");

                options[key] = hasValue ? args[++i] : "true";
            }

            return new CommandLine(command, options) { Positionals = positionals };
        }

        /// <summary>
        /// The same options under another command, without the given options. Used by
        /// <c>submit</c> to describe the command it submits.
        /// </summary>
        public CommandLine WithCommand(string command, params string[] withoutOptions)
        {
            Dictionary<string, string> options = _options
                .Where(option => !withoutOptions.Contains(option.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(option => option.Key, option => option.Value, StringComparer.OrdinalIgnoreCase);

            return new CommandLine(command.ToLowerInvariant(), options);
        }

        /// <summary>
        /// Reads a required option.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the option is absent or blank.
        /// </exception>
        public string Require(string key)
        {
            if (_options.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            throw new ArgumentException($"Missing required option --{key}");
        }

        /// <summary>
        /// Reads an optional option, or <see langword="null"/> when it was not supplied.
        /// </summary>
        public string? Optional(string key)
        {
            return _options.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        /// <summary>
        /// Reads an integer option, falling back to <paramref name="fallback"/> when it is
        /// absent or does not parse.
        /// </summary>
        public int Int(string key, int fallback)
        {
            return _options.TryGetValue(key, out string? raw) && int.TryParse(raw, out int value)
                ? value
                : fallback;
        }

        /// <summary>
        /// Whether a flag was present, regardless of any value given for it.
        /// </summary>
        public bool Flag(string key)
        {
            return _options.ContainsKey(key);
        }
    }
}
