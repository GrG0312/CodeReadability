namespace CodeReadabilityLib.Languages
{
    public class LanguageException : Exception
    {
        public LanguageException() { }
        public LanguageException(string message) : base(message) { }
    }

    public class LanguageNotFoundException : LanguageException
    {
        public LanguageNotFoundException() { }
        public LanguageNotFoundException(string message) : base(message) { }
    }
}
