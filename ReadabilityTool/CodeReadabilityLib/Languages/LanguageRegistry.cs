namespace CodeReadabilityLib.Languages
{
    /// <summary>
    /// The catalogue of programming languages this library can analyse, together with the
    /// syntax facts each one needs.
    /// </summary>
    public static class LanguageRegistry
    {
        /// <summary>
        /// Every accepted language name, for use in error messages and help text.
        /// </summary>
        public static string SupportedNames
        {
            get
            {
                return string.Join(", ", SupportedLanguages.SelectMany(l => l.LangNames).Distinct());
            }
        }

        /// <summary>
        /// Every recognised file extension, for use in error messages and help text.
        /// </summary>
        public static string SupportedExtensions
        {
            get
            {
                return string.Join(", ", SupportedLanguages.SelectMany(l => l.FileExtensions).Distinct());
            }
        }

        public static IReadOnlyCollection<SupportedLanguage> SupportedLanguages { get; } = new List<SupportedLanguage>
        {
            new SupportedLanguage
            {
                Language = ProgLang.CSharp,
                LangNames = ["c#", "csharp", "cs"],
                FileExtensions = [".cs"],
                Context = new LanguageContext
                {
                    UsesSignificantIndent = false,
                    SingleLineCommentPrefixes = new HashSet<string> { "//" },
                    MultiLineCommentDelimiters = new HashSet<(string, string)> { ("/*", "*/") },
                    BlockOpeningTokens = new HashSet<char> { '{' },
                    BlockClosingTokens = new HashSet<char> { '}' },
                    CyclomaticKeywords = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "if", "for", "while", "case", "catch"
                    },
                    Operators = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "+","-","*","/","%","=","==","!=","<",">","<=",">=",
                        "&&","||","!","??","?.","=>","+=","-=","*=","/="
                    },
                    DocumentationCommentPrefix = "///",
                    ReservedNames = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "abstract","as","base","bool","break","byte","case","catch","char",
                        "checked","class","const","continue","decimal","default","delegate",
                        "do","double","else","enum","event","explicit","extern","false",
                        "finally","fixed","float","for","foreach","goto","if","implicit",
                        "in","int","interface","internal","is","lock","long","namespace",
                        "new","null","object","operator","out","override","params","private",
                        "protected","public","readonly","ref","return","sbyte","sealed",
                        "short","sizeof","stackalloc","static","string","struct","switch",
                        "this","throw","true","try","typeof","uint","ulong","unchecked",
                        "unsafe","ushort","using","virtual","void","volatile","while"
                    }
                }
            },
            new SupportedLanguage
            {
                Language = ProgLang.Python,
                LangNames = ["python"],
                FileExtensions = [".py"],
                Context = new LanguageContext
                {
                    UsesSignificantIndent = true,
                    SingleLineCommentPrefixes = new HashSet<string> { "#" },
                    MultiLineCommentDelimiters = new HashSet<(string, string)>
                    {
                        ("\"\"\"", "\"\"\""),
                        ("'''", "'''")
                    },
                    BlockOpeningTokens = new HashSet<char>(),
                    BlockClosingTokens = new HashSet<char>(),
                    CyclomaticKeywords = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "if", "elif", "for", "while", "except", "case"
                    },
                    Operators = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "+","-","*","/","%","=","==","!=","<",">","<=",">=",
                        "and","or","not","in","is","+=","-=","*=","/="
                    },
                    // Python documents with triple-quoted blocks rather than a line prefix.
                    DocumentationBlockDelimiters = new HashSet<(string, string)>
                    {
                        ("\"\"\"", "\"\"\""),
                        ("'''", "'''")
                    },
                    ReservedNames = new HashSet<string>(StringComparer.Ordinal)
                    {
                        // True keywords.
                        "and","as","assert","async","await","break","class","continue","def",
                        "del","elif","else","except","False","finally","for","from","global",
                        "if","import","in","is","lambda","None","nonlocal","not","or","pass",
                        "raise","return","True","try","while","with","yield",

                        // Builtins that are legal to shadow but hurt readability when shadowed.
                        "all","any","bool","bytes","dict","filter","float","format","id",
                        "input","int","len","list","map","max","min","next","object","open",
                        "print","range","round","set","sorted","str","sum","tuple","type","zip"
                    }
                }
            },
            new SupportedLanguage
            {
                Language = ProgLang.Java,
                LangNames = ["java"],
                FileExtensions = [".java"],
                Context = new LanguageContext
                {
                    UsesSignificantIndent = false,
                    SingleLineCommentPrefixes = new HashSet<string> { "//" },
                    MultiLineCommentDelimiters = new HashSet<(string, string)> { ("/*", "*/") },
                    BlockOpeningTokens = new HashSet<char> { '{' },
                    BlockClosingTokens = new HashSet<char> { '}' },
                    CyclomaticKeywords = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "if", "for", "while", "case", "catch"
                    },
                    Operators = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "+","-","*","/","%","=","==","!=","<",">","<=",">=",
                        "&&","||","!","->","::","+=","-=","*=","/="
                    },
                    // Javadoc: a block comment whose opening has a second asterisk.
                    DocumentationBlockDelimiters = new HashSet<(string, string)>
                    {
                        ("/**", "*/")
                    },
                    ReservedNames = new HashSet<string>(StringComparer.Ordinal)
                    {
                        // Keywords and literals.
                        "abstract","assert","boolean","break","byte","case","catch","char",
                        "class","const","continue","default","do","double","else","enum",
                        "extends","false","final","finally","float","for","goto","if",
                        "implements","import","instanceof","int","interface","long","native",
                        "new","null","package","private","protected","public","return",
                        "short","static","strictfp","super","switch","synchronized","this",
                        "throw","throws","transient","true","try","void","volatile","while",

                        // Contextual keywords, legal as names but confusing when reused.
                        "var","record","yield","sealed","permits"
                    }
                }
            }
        };

        /// <summary>
        /// Returns the <see cref="SupportedLanguage"/> instance corresponding to the given language <paramref name="name"/>.
        /// </summary>
        /// <exception cref="LanguageNotFoundException"></exception>
        public static SupportedLanguage FromName(string name)
        {
            SupportedLanguage? language = SupportedLanguages.FirstOrDefault(lang => lang.LangNames.Contains(name, StringComparer.OrdinalIgnoreCase));

            if (language is null)
            {
                throw new LanguageNotFoundException($"'{name}' is not a supported language. Supported: {SupportedNames}.");
            }

            return language;
        }

        /// <summary>
        /// Returns the <see cref="SupportedLanguage"/> instance corresponding to the given file <paramref name="extension"/>.
        /// </summary>
        /// <exception cref="LanguageNotFoundException"></exception>
        public static SupportedLanguage FromExtension(string extension)
        {
            SupportedLanguage? language = SupportedLanguages.FirstOrDefault(lang => lang.FileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase));

            if (language is null)
            {
                throw new LanguageNotFoundException(
                    $"'{extension}' is not a supported extension. Supported: {SupportedExtensions}.");
            }

            return language;
        }
    }
}
