using CodeReadabilityLib.Languages;
using System.Text.RegularExpressions;

namespace CodeReadabilityLib.Metrics
{
    /// <summary>
    /// Central registry of all available readability metrics.
    /// Add new metrics here or in new language-specific nested classes.
    /// </summary>
    ///
    /// <remarks>
    /// Metrics come in two families. Those in <see cref="Algorithmic"/> are computed
    /// deterministically from the source text, while those in <see cref="LlmBased"/> are
    /// judgement calls delegated to a language model.
    /// </remarks>
    public static partial class MetricRegistry
    {
        [GeneratedRegex(@"\b[A-Za-z_][A-Za-z0-9_]*\b")]
        private static partial Regex IdentifierRegex();


        [GeneratedRegex(@"^[a-z_][a-z0-9_]*$")]
        private static partial Regex SnakeCaseRegex();


        [GeneratedRegex(@"^[a-z]+([A-Z][a-z0-9]*)*$")]
        private static partial Regex CamelCaseRegex();


        /// <summary>
        /// Matches a name being introduced: a Python <c>def</c> or <c>class</c>, a parameter
        /// or assignment target, or a C# declaration of the form <c>Type name =</c>.
        /// </summary>
        [GeneratedRegex(
            @"(?:\b(?:def|class)\s+(?<name>[A-Za-z_]\w*))" +
            @"|(?:\b[A-Za-z_]\w*\s+(?<name>[A-Za-z_]\w*)\s*(?:=[^=]|;|\)))" +
            @"|(?:^\s*(?<name>[A-Za-z_]\w*)\s*=[^=])",
            RegexOptions.Multiline)]
        private static partial Regex DeclarationRegex();

        public static readonly IReadOnlyCollection<AlgorithmicMetricDefinition> Algorithmic = [
            #region Formatting & Structure Metrics
            // ======================================================== //
            //             FORMATTING & STRUCTURE METRICS               //
            // ======================================================== //
            new AlgorithmicMetricDefinition()
            {
                Id = "indentation_consistency",
                Name = "Indentation Consistency",
                Description = "Percentage of indented lines whose indentation is a multiple of the language's indent unit.",
                ApplicableLanguages = [ProgLang.CSharp, ProgLang.Java],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    LanguageContext context = lang.Context;

                    // Comments are left out: block comment continuation lines (" * ...", the
                    // Javadoc convention) are aligned one space in, which is not indentation.
                    int[] indents = context.SplitCode(CodeNormalizer.Strip(code, context).Code)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(context.CountLeadingWhitespace)
                        .ToArray();

                    if (indents.Length == 0) return 100d;

                    int aligned = indents.Count(i => i % LanguageContext.INDENT_SIZE == 0);
                    return aligned * 100d / indents.Length;
                }
            },

            new AlgorithmicMetricDefinition()
            {
                Id = "average_line_length",
                Name = "Average Line Length",
                Description = "Mean character count of non-blank lines. Shorter lines generally read more easily.",
                MinValue = 0,
                HigherIsBetter = false,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    string[] lines = lang.Context.SplitCode(code).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                    return lines.Length == 0 ? 0d : lines.Average(l => l.TrimEnd().Length);
                }
            },

            new AlgorithmicMetricDefinition()
            {
                Id = "whitespace_usage",
                Name = "Whitespace Usage",
                Description = "Percentage of binary operators and commas that are separated from their operands by whitespace.",
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    // Strings and comments are removed first so that punctuation inside text
                    // is not mistaken for an operator that needs spacing.
                    string stripped = CodeNormalizer.Strip(code, lang.Context).Code;

                    int total = 0;
                    int spaced = 0;

                    // Longest operators first, so that "<=" is not counted as "<" then "=".
                    List<string> operators = lang.Context.Operators
                        .Where(op => op.Length > 0 && !char.IsLetter(op[0]))
                        .OrderByDescending(op => op.Length)
                        .ToList();

                    for (int i = 0; i < stripped.Length; i++)
                    {
                        if (stripped[i] == ',')
                        {
                            total++;

                            // A comma reads correctly when followed by a space or a line break.
                            if (i + 1 >= stripped.Length || stripped[i + 1] is ' ' or '\n' or '\r')
                            {
                                spaced++;
                            }

                            continue;
                        }

                        string? matched = operators.FirstOrDefault(
                            op => i + op.Length <= stripped.Length &&
                                  string.CompareOrdinal(stripped, i, op, 0, op.Length) == 0);

                        if (matched is null)
                        {
                            continue;
                        }

                        int before = i - 1;
                        int after = i + matched.Length;

                        // Unary usage (a leading sign, or "**" style repetition) has no left
                        // operand, so only count occurrences that sit between two tokens.
                        bool hasLeftOperand = before >= 0 && !char.IsWhiteSpace(stripped[before]);
                        bool hasRightOperand = after < stripped.Length && !char.IsWhiteSpace(stripped[after]);

                        bool spacedLeft = before < 0 || char.IsWhiteSpace(stripped[before]);
                        bool spacedRight = after >= stripped.Length || char.IsWhiteSpace(stripped[after]);

                        total++;
                        if (spacedLeft && spacedRight)
                        {
                            spaced++;
                        }
                        else if (!hasLeftOperand && !hasRightOperand)
                        {
                            // Nothing to separate; do not penalise.
                            spaced++;
                        }

                        i += matched.Length - 1;
                    }

                    return total == 0 ? 100d : spaced * 100d / total;
                }
            },
            new AlgorithmicMetricDefinition()
            {
                Id = "bracing_style_consistency",
                Name = "Bracing Style Consistency",
                Description = "How consistently opening braces are placed, as the share belonging to whichever style the file uses more often.",
                // In Python blocks are defined by indentation, so there is no brace style to measure.
                ApplicableLanguages = [ProgLang.CSharp, ProgLang.Java],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    LanguageContext context = lang.Context;
                    string[] lines = context.SplitCode(CodeNormalizer.Strip(code, context).Code);

                    // The metric is about consistency, not about which style is correct, so
                    // both conventions are counted and the dominant one defines the score.
                    int sameLine = 0;
                    int ownLine = 0;

                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();

                        if (trimmed.Length == 0 || !trimmed.Contains('{'))
                        {
                            continue;
                        }

                        if (trimmed == "{")
                        {
                            ownLine++;
                        }
                        else if (trimmed.EndsWith('{'))
                        {
                            sameLine++;
                        }
                    }

                    int total = sameLine + ownLine;

                    // Fewer than two braces cannot be inconsistent with anything.
                    return total == 0 ? 100d : Math.Max(sameLine, ownLine) * 100d / total;
                }
            },

            new AlgorithmicMetricDefinition()
            {
                Id = "nesting_depth",
                Name = "Maximum Nesting Depth",
                Description = "Deepest level of nested blocks. Deep nesting increases the state a reader must track.",
                MinValue = 0,
                HigherIsBetter = false,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    LanguageContext context = lang.Context;
                    string stripped = CodeNormalizer.Strip(code, context).Code;

                    if (context.UsesSignificantIndent)
                    {
                        int[] indents = context.SplitCode(stripped)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .Select(context.CountLeadingWhitespace)
                            .ToArray();

                        if (indents.Length == 0)
                        {
                            return 0d;
                        }

                        int baseline = indents.Min();
                        return (indents.Max() - baseline) / (double)LanguageContext.INDENT_SIZE;
                    }

                    int depth = 0, max = 0;
                    foreach (char c in stripped)
                    {
                        if (context.BlockOpeningTokens.Contains(c))
                        {
                            max = Math.Max(max, ++depth);
                        }
                        else if (context.BlockClosingTokens.Contains(c))
                        {
                            depth = Math.Max(0, depth - 1);
                        }
                    }
                    return max;
                }
            },
            #endregion
            #region Complexity Metrics
            // ======================================================== //
            //                   COMPLEXITY METRICS                     //
            // ======================================================== //
            new AlgorithmicMetricDefinition()
            {
                Id = "cyclomatic_complexity",
                Name = "Cyclomatic Complexity",
                Description = "Count of linearly independent paths, approximated by branch-keyword frequency.",
                MinValue = 1,
                HigherIsBetter = false,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    string stripped = CodeNormalizer.Strip(code, lang.Context).Code;

                    int branches = IdentifierRegex().Matches(stripped).Count(m => lang.Context.CyclomaticKeywords.Contains(m.Value));
                    return branches + 1;
                }
            },

            new AlgorithmicMetricDefinition()
            {
                Id = "halstead_volume",
                Name = "Halstead Volume",
                Description = "Program length times log2 of vocabulary size; a proxy for information content.",
                MinValue = 0,
                HigherIsBetter = false,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    string stripped = CodeNormalizer.Strip(code, lang.Context).Code;

                    List<string> operators = new List<string>();
                    foreach (string op in lang.Context.Operators.OrderByDescending(o => o.Length))
                    {
                        int index = 0;
                        while ((index = stripped.IndexOf(op, index, StringComparison.Ordinal)) >= 0)
                        {
                            operators.Add(op);
                            index += op.Length;
                        }
                    }

                    string[] operands = IdentifierRegex().Matches(stripped).Select(m => m.Value).ToArray();

                    int n1 = operators.Distinct().Count(), n2 = operands.Distinct().Count();
                    int totalLength = operators.Count + operands.Length;
                    int vocabulary = n1 + n2;

                    return vocabulary == 0 ? 0d : totalLength * Math.Log2(vocabulary);
                }
            },

            new AlgorithmicMetricDefinition()
            {
                Id = "lines_of_code",
                Name = "Lines of Code",
                Description = "Non-blank, non-comment source lines.",
                MinValue = 0,
                HigherIsBetter = false,
                Algorithm = static (code, lang) =>
                {
                    return CodeNormalizer.Strip(code, lang.Context).CodeLines;
                }
            },
            #endregion
            #region Documentation Metrics
            // ======================================================== //
            //                 DOCUMENTATION METRICS                    //
            // ======================================================== //
            new AlgorithmicMetricDefinition()
            {
                Id = "comment_density",
                Name = "Comment Density",
                Description = "Comment lines as a percentage of comment plus code lines.",
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    CodeNormalizer.Normalized n = CodeNormalizer.Strip(code, lang.Context);
                    int total = n.CodeLines + n.CommentLines;
                    return total == 0 ? 0d : n.CommentLines * 100d / total;
                }
            },

            new AlgorithmicMetricDefinition{
                Id = "docstring_density",
                Name = "Docstring Density",
                Description = "Documentation lines as a percentage of documentation plus code lines. Counts only formal documentation, not ordinary comments.",
                ApplicableLanguages = [ProgLang.Python, ProgLang.CSharp, ProgLang.Java],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    LanguageContext context = lang.Context;
                    string[] lines = context.SplitCode(code);

                    int documentationLines = 0;
                    int codeLines = 0;
                    string? closingDelimiter = null;

                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();

                        if (trimmed.Length == 0)
                        {
                            continue;
                        }

                        // Inside a documentation block, every line counts until it closes.
                        if (closingDelimiter is not null)
                        {
                            documentationLines++;

                            if (trimmed.Contains(closingDelimiter, StringComparison.Ordinal))
                            {
                                closingDelimiter = null;
                            }

                            continue;
                        }

                        // Languages such as C# mark documentation with a line prefix.
                        if (context.DocumentationCommentPrefix is { Length: > 0 } prefix &&
                            trimmed.StartsWith(prefix, StringComparison.Ordinal))
                        {
                            documentationLines++;
                            continue;
                        }

                        (string Start, string End)? block = context.DocumentationBlockDelimiters
                            .Cast<(string Start, string End)?>()
                            .FirstOrDefault(d => trimmed.StartsWith(d!.Value.Start, StringComparison.Ordinal));

                        if (block is { } opened)
                        {
                            documentationLines++;

                            // A block opened and closed on one line has its closing delimiter
                            // after the opening one; otherwise the block stays open.
                            if (!trimmed[opened.Start.Length..].Contains(opened.End, StringComparison.Ordinal))
                            {
                                closingDelimiter = opened.End;
                            }

                            continue;
                        }

                        codeLines++;
                    }

                    int total = documentationLines + codeLines;
                    return total == 0 ? 0d : documentationLines * 100d / total;
                }
            },
            #endregion
            #region Naming Metrics
            // ======================================================== //
            //                     NAMING METRICS                       //
            // ======================================================== //
            new AlgorithmicMetricDefinition()
            {
                Id = "naming_convention_adherence",
                Name = "Naming Convention Adherence",
                Description = "Percentage of identifiers matching the language's idiomatic casing.",
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    string stripped = CodeNormalizer.Strip(code, lang.Context).Code;
                    string[] identifiers = IdentifierRegex().Matches(stripped)
                        .Select(m => m.Value)
                        .Where(v => v.Length > 1 && !lang.Context.CyclomaticKeywords.Contains(v))
                        .ToArray();

                    if (identifiers.Length == 0)
                    {
                        return 100d;
                    }

                    Regex expected = lang.Language == ProgLang.Python ? SnakeCaseRegex() : CamelCaseRegex();
                    int conforming = identifiers.Count(i => expected.IsMatch(i) || char.IsUpper(i[0]));
                    return conforming * 100d / identifiers.Length;
                }
            },

            new AlgorithmicMetricDefinition()
            {
                Id = "identifier_length",
                Name = "Mean Identifier Length",
                Description = "Average identifier length; very short or very long names both hurt comprehension.",
                MinValue = 0,
                HigherIsBetter = true,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    string stripped = CodeNormalizer.Strip(code, lang.Context).Code;
                    string[] identifiers = IdentifierRegex().Matches(stripped)
                        .Select(m => m.Value)
                        .Where(v => !lang.Context.CyclomaticKeywords.Contains(v))
                        .ToArray();
                    return identifiers.Length == 0 ? 0d : identifiers.Average(i => i.Length);
                }
            },

            new AlgorithmicMetricDefinition()
            {
                Id = "reserved_name_shadowing",
                Name = "Reserved Name Shadowing",
                Description = "Number of declarations that reuse a reserved word or a well-known builtin as their own name.",
                MinValue = 0,
                HigherIsBetter = false,
                Algorithm = static (string code, SupportedLanguage lang) =>
                {
                    // Only declaration sites count. The previous version counted every
                    // occurrence of a branch keyword, so ordinary "if" and "for" statements
                    // were reported as naming violations and the metric rose with the size
                    // of the method rather than with anything about its names.
                    string stripped = CodeNormalizer.Strip(code, lang.Context).Code;

                    int shadowed = 0;

                    foreach (Match match in DeclarationRegex().Matches(stripped))
                    {
                        string name = match.Groups["name"].Value;

                        if (lang.Context.ReservedNames.Contains(name))
                        {
                            shadowed++;
                        }
                    }

                    return shadowed;
                }
            }
            #endregion
        ];

        public static readonly IReadOnlyCollection<MetricDefinition> LlmBased = [
            #region Naming Quality Metrics
            // ======================================================== //
            //                 NAMING QUALITY METRICS                   //
            // ======================================================== //
            new MetricDefinition{
                Id = "appropriateness_of_names",
                Name = "Appropriateness of Names",
                Description = "Evaluates how well the names of variables, functions, and classes reflect their purpose and usage. Appropriate naming can significantly enhance readability.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            new MetricDefinition{
                Id = "consistency_of_naming",
                Name = "Consistency of Naming",
                Description = "Assesses how consistently naming conventions are applied throughout the codebase. Consistent naming can improve readability and reduce cognitive load.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            new MetricDefinition{
                Id = "pronounceability_of_names",
                Name = "Pronounceability of Names",
                Description = "Evaluates how easily the names of variables, functions, and classes can be pronounced. Easily pronounceable names can improve readability and communication among developers.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            #endregion
            #region Comment Quality Metrics
            new MetricDefinition{
                Id = "clarity_of_comments",
                Name = "Clarity of Comments",
                Description = "Assesses how clear and informative the comments in the code are. Clear comments can help explain complex logic and improve readability.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            new MetricDefinition{
                Id = "usefulness_of_comments",
                Name = "Usefulness of Comments",
                Description = "Evaluates how useful the comments are in understanding the code. Useful comments can provide context and explanations that enhance readability.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            new MetricDefinition{
                Id = "consistency_of_comments_and_code",
                Name = "Consistency of Comments and Code",
                Description = "Assesses how well the comments align with the actual code. Inconsistent comments can lead to confusion and reduce readability.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            #endregion
            #region Complexity & Understandability Metrics
            new MetricDefinition{
                Id = "high_level_understandability",
                Name = "High-Level Understandability",
                Description = "Evaluates how easily a developer can understand the overall structure and purpose of the code. High-level understandability can improve readability and ease of navigation.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            new MetricDefinition{
                Id = "cognitive_load",
                Name = "Cognitive Load",
                Description = "Assesses the mental effort required to understand the code. Convoluted or tricky logic can increase cognitive load and reduce readability.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = false
            },
            new MetricDefinition{
                Id = "use_of_idioms",
                Name = "Use of Idioms",
                Description = "Evaluates how well the code uses common language idioms and patterns. Proper use of idioms can enhance readability by leveraging familiar constructs.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            #endregion
            #region Error Handling & Validation
            new MetricDefinition{
                Id = "error_handling_clarity",
                Name = "Error Handling Clarity",
                Description = "Assesses how clearly the code handles errors and exceptions. Clear error handling can improve readability by making it easier to understand how the code responds to unexpected conditions.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            #endregion
            #region Usage of Project-Specific Conventions
            new MetricDefinition{
                Id = "adherence_to_project_conventions",
                Name = "Adherence to Project Conventions",
                Description = "Evaluates how well the code follows any specific conventions or guidelines established for similar projects. Adherence to project conventions can enhance readability by providing a consistent style and structure.",
                ApplicableLanguages = [],
                MinValue = 0,
                MaxValue = 100,
                HigherIsBetter = true
            },
            #endregion
        ];

        #region Query Methods

        /// <summary>
        /// Every registered metric, algorithmic and LLM-based alike.
        /// </summary>
        public static IEnumerable<MetricDefinition> GetAllMetrics()
        {
            return Algorithmic.Concat(LlmBased);
        }

        /// <summary>
        /// Every metric applicable to <paramref name="language"/>.
        /// </summary>
        public static IEnumerable<MetricDefinition> GetMetricsForLanguage(ProgLang language)
        {
            return GetAllMetrics().Where(metric => metric.AppliesTo(language));
        }

        /// <summary>
        /// The deterministically computed metrics applicable to <paramref name="language"/>.
        /// </summary>
        ///
        /// <remarks>
        /// The return type is deliberately <see cref="AlgorithmicMetricDefinition"/> rather
        /// than <see cref="MetricDefinition"/>. Callers need the
        /// <see cref="AlgorithmicMetricDefinition.Algorithm"/> delegate, and returning the
        /// base type would force them into a downcast that only fails at run time.
        /// </remarks>
        public static IEnumerable<AlgorithmicMetricDefinition> GetAlgorithmicForLanguage(ProgLang language)
        {
            return Algorithmic.Where(metric => metric.AppliesTo(language));
        }

        /// <summary>
        /// The LLM-scored metrics applicable to <paramref name="language"/>.
        /// </summary>
        public static IEnumerable<MetricDefinition> GetLlmBasedForLanguage(ProgLang language)
        {
            return LlmBased.Where(metric => metric.AppliesTo(language));
        }

        /// <summary>
        /// Looks up a metric by its identifier.
        /// </summary>
        ///
        /// <returns>
        /// The matching definition, or <see langword="null"/> when no metric has that id.
        /// </returns>
        public static MetricDefinition? GetMetricById(string id)
        {
            return GetAllMetrics().FirstOrDefault(metric => metric.Id == id);
        }

        #endregion
    }
}
