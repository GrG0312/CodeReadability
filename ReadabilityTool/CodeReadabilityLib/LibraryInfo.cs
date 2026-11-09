namespace CodeReadabilityLib
{
    /// <summary>
    /// Assembly-wide constants describing the library itself.
    /// </summary>
    ///
    /// <remarks>
    /// Replaces the version constant that used to live on the old <c>Reposcraper</c>
    /// orchestrator, so that identifying the library no longer requires a reference to
    /// a pipeline class.
    /// </remarks>
    public static class LibraryInfo
    {
        /// <summary>
        /// The library version, sent as part of the User-Agent header on outbound requests.
        /// </summary>
        public const string Version = "1.2.0";

        /// <summary>
        /// The User-Agent value identifying this library to remote hosts.
        /// </summary>
        public const string UserAgent = "CodeReadabilityLib/" + Version;
    }
}
