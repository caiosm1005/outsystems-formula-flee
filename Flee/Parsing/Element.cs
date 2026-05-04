namespace Flee.Parsing
{
    /// <summary>
    /// Common base class for regular-expression elements, i.e. the building blocks of a parsed
    /// regular expression (literal strings, character sets, alternatives, repetitions, etc.).
    /// </summary>
    internal abstract class Element : ICloneable
    {
        /// <summary>
        /// Creates a copy of this element. Implementations may return the same instance when
        /// the element is immutable.
        /// </summary>
        /// <returns>The copied element.</returns>
        public abstract object Clone();

        /// <summary>
        /// Attempts to match this element starting at <paramref name="start"/> in
        /// <paramref name="buffer"/>.
        /// </summary>
        /// <param name="m">The matcher tracking case sensitivity and end-of-stream state.</param>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <param name="start">The starting position in the buffer.</param>
        /// <param name="skip">The number of matches to skip before returning one.</param>
        /// <returns>The length of the match, or <c>-1</c> if no match was found.</returns>
        public abstract int Match(Matcher m,
                                  ReaderBuffer buffer,
                                  int start,
                                  int skip);

        /// <summary>
        /// Writes a textual description of this element to <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The text writer that receives the description.</param>
        /// <param name="indent">The indentation prefix applied to each line.</param>
        public abstract void PrintTo(TextWriter output, string indent);
    }
}
