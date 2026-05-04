using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Process-wide cache for compiled <see cref="Regex"/> instances. Used by the IL emitted
    /// for <c>MATCH</c>, <c>LIKE</c>, and the regex form of <c>CONTAINS</c> so the regex is
    /// constructed once per (pattern, options) pair instead of once per evaluation.
    /// </summary>
    internal static class RegexCache
    {
        private static readonly ConcurrentDictionary<(string pattern, int options), Regex> _cache = new();

        /// <summary>
        /// Returns the cached <see cref="Regex"/> for <paramref name="pattern"/> and
        /// <paramref name="options"/>, constructing it on first use.
        /// </summary>
        /// <param name="pattern">The .NET regex pattern.</param>
        /// <param name="options">The <see cref="RegexOptions"/> bitmask.</param>
        /// <returns>The cached regex.</returns>
        public static Regex Get(string pattern, int options)
        {
            return _cache.GetOrAdd((pattern, options), key => new Regex(key.pattern, (RegexOptions)key.options));
        }
    }
}
