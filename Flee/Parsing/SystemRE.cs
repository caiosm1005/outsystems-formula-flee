using System.Text.RegularExpressions;

namespace Flee.Parsing
{
    internal class SystemRE(string regex, bool ignoreCase) : REHandler
    {
        private readonly Regex _reg = ignoreCase ? new Regex(regex, RegexOptions.IgnoreCase) : new Regex(regex);

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
