namespace CodeReadabilityLib.Languages
{
    /// <summary>
    /// The syntax facts about one language that the algorithmic metrics need in order to
    /// measure source text without parsing it.
    /// </summary>
    public record LanguageContext
    {
        public const int INDENT_SIZE = 4;
        public const int TABULATOR_SIZE = 4;

        /// <summary>
        /// Whether indentation defines block structure, as in Python, rather than being
        /// purely cosmetic. Metrics that measure nesting branch on this.
        /// </summary>
        public required bool UsesSignificantIndent { get; init; }

        public required IReadOnlySet<string> SingleLineCommentPrefixes { get; init; }
        public required IReadOnlySet<(string Start, string End)> MultiLineCommentDelimiters { get; init; }

        public required IReadOnlySet<char> BlockOpeningTokens { get; init; }
        public required IReadOnlySet<char> BlockClosingTokens { get; init; }

        /// <summary>
        /// Keywords that introduce a branch, and therefore add one to cyclomatic complexity.
        /// </summary>
        public required IReadOnlySet<string> CyclomaticKeywords { get; init; }

        public required IReadOnlySet<string> Operators { get; init; }

        /// <summary>
        /// The prefix that marks a documentation comment, such as <c>///</c> in C#.
        /// </summary>
        ///
        /// <remarks>
        /// <see langword="null"/> for languages whose documentation uses the ordinary
        /// multi-line comment delimiters instead, as Python's docstrings do.
        /// </remarks>
        public string? DocumentationCommentPrefix { get; init; }

        /// <summary>
        /// Delimiters that open and close a documentation block, such as Python's triple
        /// quotes or Java's <c>/** ... */</c>.
        /// </summary>
        public IReadOnlySet<(string Start, string End)> DocumentationBlockDelimiters { get; init; } = new HashSet<(string, string)>();

        /// <summary>
        /// Names that already mean something in the language, and which a well-written
        /// program should therefore not reuse as identifiers.
        /// </summary>
        ///
        /// <remarks>
        /// Includes both true reserved words and widely-used builtins. Shadowing a builtin
        /// such as Python's <c>list</c> is legal but hurts readability, which is what the
        /// shadowing metric is meant to catch.
        /// </remarks>
        public IReadOnlySet<string> ReservedNames { get; init; } = new HashSet<string>();

        /// <summary>
        /// Splits the given <paramref name="code"/> into lines, normalizing line endings to '\n'.
        /// </summary>
        /// 
        /// <returns>
        /// An array of strings, where each string is a line of code from the input <paramref name="code"/>.
        /// </returns>
        public string[] SplitCode(string code)
        {
            return code
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');
        }

        /// <summary>
        /// Visual width of a line's leading whitespace, using real tab-stop semantics.
        /// </summary>
        public int CountLeadingWhitespace(string line)
        {
            int width = 0;

            foreach (char c in line)
            {
                if (c == ' ')
                {
                    width++;
                }
                else if (c == '\t')
                {
                    width += TABULATOR_SIZE - (width % TABULATOR_SIZE);
                }
                else break;
            }

            return width;
        }
    }
}
