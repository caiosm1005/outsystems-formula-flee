using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Runtime helpers for the SQL <c>LIKE</c> operator. Translates a SQL wildcard pattern
    /// into a .NET regex (cached per pattern) and tests it against the input. Match is
    /// always case-insensitive to mirror SQL Server / OutSystems default semantics.
    /// </summary>
    internal static class LikeRuntime
    {
        private static readonly ConcurrentDictionary<string, Regex> _cache = new();

        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        /// <summary>
        /// Returns whether <paramref name="input"/> matches the SQL <paramref name="pattern"/>.
        /// </summary>
        /// <param name="input">The string to test (may be <see langword="null"/> — never matches).</param>
        /// <param name="pattern">The SQL pattern with wildcards <c>%</c>, <c>_</c>, <c>[abc]</c>, <c>[^abc]</c>.</param>
        /// <returns><see langword="true"/> when the input matches the pattern.</returns>
        public static bool IsMatch(string? input, string? pattern)
        {
            if (input == null || pattern == null)
            {
                return false;
            }

            Regex regex = _cache.GetOrAdd(pattern, p => new Regex(SqlPatternToRegex(p), Options));
            return regex.IsMatch(input);
        }

        /// <summary>
        /// Translates a SQL <c>LIKE</c> pattern to an anchored .NET regex. <c>%</c> becomes
        /// <c>.*</c>, <c>_</c> becomes <c>.</c>, <c>[…]</c> and <c>[^…]</c> pass through, and
        /// every other regex metacharacter is escaped.
        /// </summary>
        /// <param name="pattern">The SQL pattern.</param>
        /// <returns>The equivalent .NET regex pattern.</returns>
        public static string SqlPatternToRegex(string pattern)
        {
            StringBuilder sb = new(pattern.Length + 4);
            sb.Append('^');

            int i = 0;
            while (i < pattern.Length)
            {
                char c = pattern[i];
                switch (c)
                {
                    case '%':
                        sb.Append(".*");
                        i++;
                        break;
                    case '_':
                        sb.Append('.');
                        i++;
                        break;
                    case '[':
                        int close = pattern.IndexOf(']', i + 1);
                        if (close < 0)
                        {
                            sb.Append(Regex.Escape("["));
                            i++;
                        }
                        else
                        {
                            sb.Append(pattern, i, close - i + 1);
                            i = close + 1;
                        }
                        break;
                    default:
                        sb.Append(Regex.Escape(c.ToString()));
                        i++;
                        break;
                }
            }

            sb.Append('$');
            return sb.ToString();
        }
    }
}
