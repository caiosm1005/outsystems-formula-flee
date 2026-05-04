namespace Flee.Parsing
{
    /// <summary>
    /// A regular-expression string element. Matches an exact string. The string is immutable
    /// once the element has been constructed.
    /// </summary>
    /// <param name="str">The string to match.</param>
    internal class StringElement(string str) : Element
    {
        private readonly string _value = str;

        /// <summary>
        /// Initializes a new <see cref="StringElement"/> matching a single character.
        /// </summary>
        /// <param name="c">The character to match.</param>
        public StringElement(char c)
            : this(c.ToString())
        {
        }

        /// <summary>
        /// Returns the matched string.
        /// </summary>
        /// <returns>The string value.</returns>
        public string GetString()
        {
            return _value;
        }

        /// <summary>
        /// Returns this element unchanged. String elements are immutable, so a shared
        /// reference is sufficient.
        /// </summary>
        /// <returns>This instance.</returns>
        public override object Clone()
        {
            return this;
        }

        /// <summary>
        /// Matches the configured string starting at <paramref name="start"/>.
        /// </summary>
        /// <param name="m">The matcher tracking case sensitivity and end-of-stream state.</param>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <param name="start">The starting position in the buffer.</param>
        /// <param name="skip">The number of matches to skip before returning one.</param>
        /// <returns>The string length on a match, or <c>-1</c> otherwise.</returns>
        public override int Match(Matcher m,
                                  ReaderBuffer buffer,
                                  int start,
                                  int skip)
        {
            if (skip != 0)
            {
                return -1;
            }
            for (int i = 0; i < _value.Length; i++)
            {
                var c = buffer.Peek(start + i);
                if (c < 0)
                {
                    m.SetReadEndOfString();
                    return -1;
                }
                if (m.IsCaseInsensitive())
                {
                    c = Char.ToLower((char)c);
                }
                if (c != _value[i])
                {
                    return -1;
                }
            }
            return _value.Length;
        }

        /// <summary>
        /// Writes a textual description of this element to <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The text writer that receives the description.</param>
        /// <param name="indent">The indentation prefix to apply to the line.</param>
        public override void PrintTo(TextWriter output, string indent)
        {
            output.WriteLine(indent + "'" + _value + "'");
        }
    }
}
