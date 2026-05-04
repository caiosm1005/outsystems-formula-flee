namespace Flee.Parsing
{
    /// <summary>
    /// A regular-expression alternative element. This element matches the longest of two
    /// alternative subexpressions.
    /// </summary>
    /// <param name="first">The first alternative.</param>
    /// <param name="second">The second alternative.</param>
    internal class AlternativeElement(Element first, Element second) : Element
    {
        private readonly Element _elem1 = first;
        private readonly Element _elem2 = second;

        /// <summary>
        /// Returns this element unchanged. The alternatives are treated as immutable, so a
        /// shallow clone is sufficient.
        /// </summary>
        /// <returns>A new <see cref="AlternativeElement"/> wrapping the same alternatives.</returns>
        public override object Clone()
        {
            return new AlternativeElement(_elem1, _elem2);
        }

        /// <summary>
        /// Returns the length of the longest match starting at <paramref name="start"/>, taking
        /// the <paramref name="skip"/> hint into account so callers can iterate through
        /// successive matches.
        /// </summary>
        /// <param name="m">The matcher tracking case sensitivity and end-of-stream state.</param>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <param name="start">The starting position in the buffer.</param>
        /// <param name="skip">The number of matches to skip before returning one.</param>
        /// <returns>The length of the chosen match, or <c>-1</c> if no match was found.</returns>
        public override int Match(Matcher m,
                                  ReaderBuffer buffer,
                                  int start,
                                  int skip)
        {
            int length = 0;
            int skip1 = 0;
            int skip2 = 0;

            while (length >= 0 && skip1 + skip2 <= skip)
            {
                int length1 = _elem1.Match(m, buffer, start, skip1);
                int length2 = _elem2.Match(m, buffer, start, skip2);
                if (length1 >= length2)
                {
                    length = length1;
                    skip1++;
                }
                else
                {
                    length = length2;
                    skip2++;
                }
            }
            return length;
        }

        /// <summary>
        /// Writes a textual description of this element and its alternatives to
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The text writer that receives the description.</param>
        /// <param name="indent">The indentation prefix to apply to each line.</param>
        public override void PrintTo(TextWriter output, string indent)
        {
            output.WriteLine(indent + "Alternative 1");
            _elem1.PrintTo(output, indent + "  ");
            output.WriteLine(indent + "Alternative 2");
            _elem2.PrintTo(output, indent + "  ");
        }
    }
}
