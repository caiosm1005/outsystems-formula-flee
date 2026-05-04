using System.Text.RegularExpressions;

namespace Flee.Parsing
{
    /// <summary>
    /// <see cref="REHandler"/> implementation backed by the .NET
    /// <see cref="System.Text.RegularExpressions.Regex"/> engine. Used for patterns the
    /// Grammatica regex engine cannot compile.
    /// </summary>
    /// <param name="regex">The regex source.</param>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    internal class SystemRE(string regex, bool ignoreCase) : REHandler
    {
        private readonly Regex _reg = ignoreCase ? new Regex(regex, RegexOptions.IgnoreCase) : new Regex(regex);

        /// <summary>
        /// Attempts to match the regex at the current position of <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <returns>The match length, or zero when no match was found.</returns>
        public override int Match(ReaderBuffer buffer)
        {
            Match m;

            // Ugly hack since .NET doesn't have a flag for when the
            // end of the input string was encountered...
            _ = buffer.Peek(1024 * 16);
            // Also, there is no API to limit the search to the specified
            // position, so we double-check the index afterwards instead.
            m = _reg.Match(buffer.ToString(), buffer.Position);
            return m.Success && m.Index == buffer.Position ? m.Length : 0;
        }
    }
}
