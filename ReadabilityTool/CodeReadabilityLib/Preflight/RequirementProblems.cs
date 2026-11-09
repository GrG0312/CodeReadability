namespace CodeReadabilityLib.Preflight
{
    /// <summary>
    /// Collects unmet requirements, so that every problem is reported at once rather than
    /// one per attempt.
    /// </summary>
    public sealed class RequirementProblems
    {
        private readonly List<string> _problems = new List<string>();

        public IReadOnlyList<string> All => _problems;

        public bool Any => _problems.Count > 0;

        public void Add(string problem)
        {
            _problems.Add(problem);
        }

        /// <exception cref="RequirementsNotMetException">
        /// Thrown when any problem was recorded.
        /// </exception>
        public void ThrowIfAny(string command)
        {
            if (Any)
            {
                throw new RequirementsNotMetException(command, _problems);
            }
        }
    }
}
