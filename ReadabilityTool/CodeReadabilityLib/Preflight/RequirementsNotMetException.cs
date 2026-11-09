namespace CodeReadabilityLib.Preflight
{
    /// <summary>
    /// Thrown before any work starts, when one or more requirements of a command are not met.
    /// </summary>
    public sealed class RequirementsNotMetException : Exception
    {
        /// <summary>
        /// Every unmet requirement, one message each.
        /// </summary>
        public IReadOnlyList<string> Problems { get; }

        public RequirementsNotMetException(string command, IReadOnlyList<string> problems)
            : base($"{command} cannot start:" + Environment.NewLine + string.Join(Environment.NewLine, problems.Select(p => "  - " + p)))
        {
            Problems = problems;
        }
    }
}
