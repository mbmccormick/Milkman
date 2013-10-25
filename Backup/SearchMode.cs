
namespace IronCow
{
    /// <summary>
    /// The search mode for task queries.
    /// </summary>
    public enum SearchMode
    {
        /// <summary>
        /// Searches the local (cached) tasks first, and searches
        /// on the server if an error is encountered.
        /// </summary>
        LocalAndRemote,
        /// <summary>
        /// Always searches on the server.
        /// </summary>
        RemoteOnly,
        /// <summary>
        /// Always searches the local (cached) tasks only.
        /// </summary>
        LocalOnly
    }
}
