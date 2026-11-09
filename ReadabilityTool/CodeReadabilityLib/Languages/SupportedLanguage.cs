using CodeReadabilityLib.Languages;

namespace CodeReadabilityLib.Languages
{
    public record SupportedLanguage
    {
        public required ProgLang Language { get; init; }
        public required IReadOnlyCollection<string> LangNames { get; init; }
        public required IReadOnlyCollection<string> FileExtensions { get; init; }
        public required LanguageContext Context { get; init; }
    }
}
