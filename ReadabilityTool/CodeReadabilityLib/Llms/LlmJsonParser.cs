using System.Formats.Tar;
using System.Text.Json;

namespace CodeReadabilityLib.Llms
{
    /// <summary>
    /// Static class for parsing JSON strings from a raw input string.
    /// </summary>
    public static class LlmJsonParser
    {
        /// <summary>
        /// Attempts to parse a JSON string from the <paramref name="raw"/> input string.
        /// </summary>
        /// 
        /// <param name="raw">
        /// The raw input string that may contain a JSON string.
        /// </param>
        /// 
        /// <param name="json">
        /// When this method returns, contains the parsed JSON string if the parsing was successful; otherwise, an empty string.
        /// </param>
        /// 
        /// <returns>
        /// <see langword="true"/> if a JSON string was successfully parsed from the <paramref name="raw"/> input string; otherwise, <see langword="false"/>. 
        /// </returns>
        public static bool TryExtractJsonString(string raw, out string json)
        {
            json = string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            // Convert to span for efficient operations
            ReadOnlySpan<char> text = raw.AsSpan().Trim();

            // The replies will usually be in a code block
            if (text.StartsWith("```"))
            {
                // Skip the initial code block marker
                int firstNewLine = text.IndexOf('\n');
                if (firstNewLine >= 0)
                {
                    text = text[(firstNewLine + 1)..];
                }

                // If it is a code block, remove the closing marker too
                int indexOfClosingCodeBlock = text.IndexOf("```");
                if (indexOfClosingCodeBlock >= 0)
                {
                    text = text[..indexOfClosingCodeBlock];
                }
            }

            // If no opening brace is found then its an invalid JSON string
            int startingBraceIndex = text.IndexOf('{');
            if (startingBraceIndex < 0)
            {
                return false;
            }

            // Now we need to find the matching closing brace,
            // Pay attention to nested & in-string braces
            int depth = 1;
            bool inString = false;
            bool escaped = false;

            // Iterate through all characters
            for (int i = startingBraceIndex + 1; i < text.Length; i++)
            {
                char c = text[i];

                // If in the previous step
                // we encountered a backslash,
                // then we should skip this character
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                // If a backslash is encountered
                // then it is an escaped character,
                // so we should skip the next character
                if (c == '\\' && inString)
                {
                    escaped = true;
                    continue;
                }

                // If a double quote is encountered,
                // then we are either entering or exiting a string
                // Either way we ignore this character,
                // and toggle the inString flag
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                // If we are in a string,
                // then we should ignore any braces / characters
                if (inString)
                {
                    continue;
                }

                // If we are not in a string,
                // and we encounter an opening brace,
                // increase the depth counter
                if (c == '{')
                {
                    depth++;
                }

                // If we encounter a closing brace,
                // then we decrease the depth counter
                // ( the first brace was skipped becase we started the loop at startingBraceIndex + 1 )
                // If the depth counter reaches 0, then we have found the matching closing brace
                else if (c == '}' && --depth == 0)
                {
                    // QUESTION: Is the i + 1 correct here?
                    // What if we are at text.Length - 1 and the closing brace is the last character?
                    // Then this would be out of range, and would throw an exception.
                    json = text[startingBraceIndex..(i + 1)].ToString();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to parse a JSON string from the <paramref name="raw"/> input string and deserialize it into an object of type <typeparamref name="TResult"/>.
        /// </summary>
        /// 
        /// <typeparam name="TResult">
        /// The type of the object to deserialize the JSON string into.
        /// </typeparam>
        /// 
        /// <param name="raw">
        /// The raw input string that may contain a JSON string.
        /// </param>
        /// 
        /// <param name="options">
        /// The options to use when deserializing the JSON string.
        /// </param>
        /// 
        /// <param name="result">
        /// When this method returns, contains the deserialized object of type <typeparamref name="TResult"/>
        /// if the parsing and deserialization were successful; otherwise, the default value of <typeparamref name="TResult"/>.
        /// </param>
        /// 
        /// <returns>
        /// <see langword="true"/> if a JSON string was successfully parsed from the <paramref name="raw"/>
        /// input string and deserialized into an object of type <typeparamref name="TResult"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryParse<TResult>(string raw, JsonSerializerOptions options, out TResult? result)
        {
            result = default;

            // If a valid JSON string cannot be extracted from the raw input, return false
            if (!TryExtractJsonString(raw, out string json))
            {
                return false;
            }

            // Try to deserialize the extracted JSON string into the specified type T
            // If possible, return true and set the result to the deserialized object
            // Otherwise return false
            try
            {
                result = JsonSerializer.Deserialize<TResult>(json, options);
                return result is not null;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
