namespace Flee.Parsing
{
    /// <summary>
    /// Common base class for regular-expression match handlers, abstracting the underlying
    /// engine (Grammatica or .NET <see cref="System.Text.RegularExpressions.Regex"/>) used by
    /// <see cref="RegExpMatcher"/>.
    /// </summary>
    internal abstract class REHandler
    {
        /// <summary>
        /// Attempts to match the regular expression at the current position of
        /// <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The reader buffer to read characters from.</param>
        /// <returns>The number of matched characters, or zero if no match was found.</returns>
        public abstract int Match(ReaderBuffer buffer);
    }
}
