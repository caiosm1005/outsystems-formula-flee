using System.Collections;
using System.Text.RegularExpressions;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Runtime helpers for the <c>CONTAINS</c> family of operators. All overloads accept
    /// non-generic <see cref="IEnumerable"/> so the IL emitter can call into a single helper
    /// regardless of the collection's compile-time element type.
    /// </summary>
    internal static class ContainsRuntime
    {
        /// <summary>
        /// Returns whether <paramref name="value"/> appears in <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to search.</param>
        /// <param name="value">The value to find.</param>
        /// <returns><see langword="true"/> when at least one element equals <paramref name="value"/>.</returns>
        public static bool Has(IEnumerable? collection, object? value)
        {
            if (collection == null)
            {
                return false;
            }

            foreach (object? item in collection)
            {
                if (Equals(item, value))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether at least one element of <paramref name="values"/> appears in
        /// <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to search.</param>
        /// <param name="values">The values to test.</param>
        /// <returns><see langword="true"/> when any value is found.</returns>
        public static bool AnyIn(IEnumerable? collection, IEnumerable? values)
        {
            if (collection == null || values == null)
            {
                return false;
            }

            foreach (object? v in values)
            {
                if (Has(collection, v))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether every element of <paramref name="values"/> appears in
        /// <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to search.</param>
        /// <param name="values">The values to test.</param>
        /// <returns><see langword="true"/> when every value is found.</returns>
        public static bool AllIn(IEnumerable? collection, IEnumerable? values)
        {
            if (collection == null || values == null)
            {
                return false;
            }

            foreach (object? v in values)
            {
                if (!Has(collection, v))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns whether at least one string element of <paramref name="collection"/>
        /// matches <paramref name="regex"/>.
        /// </summary>
        /// <param name="collection">The collection to search.</param>
        /// <param name="regex">The regex to test against each string element.</param>
        /// <returns><see langword="true"/> when any element matches.</returns>
        public static bool AnyMatches(IEnumerable? collection, Regex regex)
        {
            if (collection == null)
            {
                return false;
            }

            foreach (object? item in collection)
            {
                if (item is string s && regex.IsMatch(s))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
