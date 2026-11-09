using CodeReadabilityLib.Languages;
using System.Text;

namespace CodeReadabilityLib.Metrics
{
    /// <summary>
    /// Strips comments and string literals so that token-counting metrics are not fooled by
    /// operators or keywords appearing inside text.
    /// </summary>
    public sealed class CodeNormalizer
    {
        /// <summary>
        /// Represents the result of normalizing a source code file.
        /// </summary>
        public sealed record Normalized
        {
            /// <summary>
            /// The normalized code, with comments and string literals removed.
            /// </summary>
            public required string Code { get; init; }

            /// <summary>
            /// The number of comment lines in the original source code.
            /// </summary>
            public required int CommentLines { get; init; }

            /// <summary>
            /// The number of code lines in the original source code.
            /// </summary>
            public required int CodeLines { get; init; }

            /// <summary>
            /// The number of blank lines in the original source code.
            /// </summary>
            public required int BlankLines { get; init; }
        }

        /// <summary>
        /// Strips the code from comments and string literals.
        /// </summary>
        /// 
        /// <param name="source">
        /// The source code to normalize.
        /// </param>
        /// 
        /// <param name="context">
        /// The <see cref="LanguageContext"/> that defines the comment and string literal syntax for the source code.
        /// </param>
        /// 
        /// <returns>
        /// Returns a <see cref="Normalized"/> result containing the stripped code and counts of comment, code, and blank lines.
        /// </returns>
        public static Normalized Strip(string source, LanguageContext context)
        {
            string[] lines = context.SplitCode(source);
            StringBuilder output = new StringBuilder(source.Length);

            int commentLines = 0;
            int codeLines = 0;
            int blankLines = 0;


            (string Start, string End)? openBlock = null;

            foreach (string line in lines)
            {
                // Variable to store the remaining part of the line to process (or to keep)
                string remaining = line;
                StringBuilder kept = new StringBuilder(line.Length);
                // To keep track of whether we have seen a comment in this line, which will help us count comment lines correctly.
                bool sawComment = false;

                // Process the line until there is nothing left to process
                while (remaining.Length > 0)
                {
                    // If the openBlock is not null, we are inside a comment block
                    if (openBlock is { } block)
                    {
                        // Find the closing delimiter for the comment block
                        int close = remaining.IndexOf(block.End, StringComparison.Ordinal);
                        // Set sawComment to true since we are inside a comment block
                        sawComment = true;

                        // If we can't find the closing delimiter, we will consider the rest of the line as part of the comment block
                        if (close < 0)
                        {
                            remaining = string.Empty;
                            break;
                        }

                        // Otherwise skip the comment block and continue processing the rest of the line
                        remaining = remaining[(close + block.End.Length)..];
                        openBlock = null;
                        continue;
                    }

                    // If we are not inside a comment block, we need to find the next comment or string literal

                    // The index of the next comment or string literal
                    int cut = -1;
                    // Delimiter for a multi-line comment if we find one
                    (string Start, string End)? starting = null;
                    // Track if is a single line comment
                    bool isLineComment = false;

                    // See if there is a single line comment in the remaining text
                    foreach (string prefix in context.SingleLineCommentPrefixes)
                    {
                        // If the index of the prefix is found and is less than the current cut, update cut and set isLineComment to true
                        int index = remaining.IndexOf(prefix, StringComparison.Ordinal);

                        if (index >= 0 && (cut < 0 || index < cut))
                        {
                            cut = index;
                            isLineComment = true;
                            starting = null;
                        }
                    }

                    // See if there is a multi-line comment in the remaining text
                    foreach ((string start, string end) in context.MultiLineCommentDelimiters)
                    {
                        // If the index of the start delimiter is found and is less than the current cut, update cut and set isLineComment to false
                        // Also set the starting variable to the current start and end delimiters
                        int index = remaining.IndexOf(start, StringComparison.Ordinal);

                        if (index >= 0 && (cut < 0 || index < cut))
                        {
                            cut = index;
                            isLineComment = false;
                            starting = (start, end);
                        }
                    }

                    // See if there is a string literal in the remaining text
                    int quote = FindQuote(remaining, cut);
                    if (quote >= 0 && (cut < 0 || quote < cut))
                    {
                        kept.Append(remaining.AsSpan(0, quote));
                        kept.Append("\"\"");
                        remaining = SkipStringLiteral(remaining, quote);
                        continue;
                    }

                    // If cut < 0, aka no comment or string literal was found, we can keep the rest of the line and break out of the loop
                    if (cut < 0)
                    {
                        kept.Append(remaining);
                        break;
                    }

                    // Save the kept part of the line up to the cut index, and set sawComment to true since we found a comment
                    kept.Append(remaining.AsSpan(0, cut));
                    sawComment = true;

                    // If we found a single line comment, we can discard the rest of the line and break out of the loop
                    if (isLineComment)
                    {
                        remaining = string.Empty;
                        break;
                    }

                    // If we found a multi-line comment, we need to find the closing delimiter and continue processing the rest of the line
                    (string s, string e) = starting!.Value;
                    string rest = remaining[(cut + s.Length)..];
                    int closing = rest.IndexOf(e, StringComparison.Ordinal);

                    if (closing < 0)
                    {
                        openBlock = (s, e);
                        remaining = string.Empty;
                        break;
                    }

                    remaining = rest[(closing + e.Length)..];
                }

                string stripped = kept.ToString();
                output.AppendLine(stripped);

                if (string.IsNullOrWhiteSpace(stripped) && sawComment) commentLines++;
                else if (string.IsNullOrWhiteSpace(stripped)) blankLines++;
                else codeLines++;
            }

            return new Normalized()
            {
                Code = output.ToString(),
                CommentLines = commentLines,
                CodeLines = codeLines,
                BlankLines = blankLines
            };
        }

        /// <summary>
        /// Finds the index of the first quote character (single or double) in the given text, up to a specified limit.
        /// </summary>
        /// 
        /// <param name="text">
        /// The text to search for a quote character.
        /// </param>
        /// 
        /// <param name="limit">
        /// The maximum index to search for a quote character. If the limit is negative, the entire text will be searched.
        /// </param>
        /// 
        /// <returns>
        /// The index of the first quote character found in the text, or -1 if no quote character is found within the specified limit.
        /// </returns>
        private static int FindQuote(string text, int limit)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (limit >= 0 && i >= limit) return -1;
                if (text[i] is '"' or '\'') return i;
            }
            return -1;
        }

        /// <summary>
        /// Skips over a string literal in the given <paramref name="text"/>, starting from the specified index <paramref name="start"/>.
        /// </summary>
        /// 
        /// <returns>
        /// Returns the remaining text after the string literal.
        /// </returns>
        private static string SkipStringLiteral(string text, int start)
        {
            char quote = text[start];
            for (int i = start + 1; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (text[i] == quote)
                {
                    return text[(i + 1)..];
                }
            }
            return string.Empty;
        }
    }
}
