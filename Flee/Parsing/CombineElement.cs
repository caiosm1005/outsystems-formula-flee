namespace Flee.Parsing
{
    /// <summary>
    /// A regular-expression element that concatenates two subexpressions, requiring both to
    /// match in sequence.
    /// </summary>
    /// <param name="first">The first subexpression.</param>
    /// <param name="second">The second subexpression.</param>
    internal class CombineElement(Element first, Element second) : Element
    {
        private readonly Element _elem1 = first;
        private readonly Element _elem2 = second;

        /// <summary>
        /// Returns a shallow clone wrapping the same subexpressions.
        /// </summary>
        /// <returns>A new <see cref="CombineElement"/>.</returns>
        public override object Clone()
        {
            return new CombineElement(_elem1, _elem2);
        }

        /// <summary>
        /// Matches both subexpressions in sequence starting at <paramref name="start"/>,
        /// returning the combined match length.
        /// </summary>
        /// <param name="m">The matcher tracking case sensitivity and end-of-stream state.</param>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <param name="start">The starting position in the buffer.</param>
        /// <param name="skip">The number of matches to skip before returning one.</param>
        /// <returns>The combined match length, or <c>-1</c> if no match was found.</returns>
        public override int Match(Matcher m,
                                  ReaderBuffer buffer,
                                  int start,
                                  int skip)
        {
            int length1 = -1;
            int length2 = 0;
            int skip1 = 0;
            int skip2 = 0;

            while (skip >= 0)
            {
                length1 = _elem1.Match(m, buffer, start, skip1);
                if (length1 < 0)
                {
                    return -1;
                }
                length2 = _elem2.Match(m, buffer, start + length1, skip2);
                if (length2 < 0)
                {
                    skip1++;
                    skip2 = 0;
                }
                else
                {
                    skip2++;
                    skip--;
                }
            }

            return length1 + length2;
        }

        /// <summary>
        /// Writes a textual description of both subexpressions to <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The text writer that receives the description.</param>
        /// <param name="indent">The indentation prefix to apply to each line.</param>
        public override void PrintTo(TextWriter output, string indent)
        {
            _elem1.PrintTo(output, indent);
            _elem2.PrintTo(output, indent);
        }
    }
}
