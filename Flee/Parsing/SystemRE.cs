using System.Text.RegularExpressions;

namespace Flee.Parsing
{
    internal class SystemRE : REHandler
    {
        private readonly Regex _reg;

        public SystemRE(string regex, bool ignoreCase)
        {
            if (ignoreCase)
            {
                _reg = new Regex(regex, RegexOptions.IgnoreCase);
            }
            else
            {
                _reg = new Regex(regex);
            }
        }

        public override int Match(ReaderBuffer buffer)
        {
            Match m;

            // Ugly hack since .NET doesn't have a flag for when the
            // end of the input string was encountered...
            buffer.Peek(1024 * 16);
            // Also, there is no API to limit the search to the specified
            // position, so we double-check the index afterwards instead.
            m = _reg.Match(buffer.ToString(), buffer.Position);
            if (m.Success && m.Index == buffer.Position)
            {
                return m.Length;
            }
            else
            {
                return 0;
            }
        }
    }
}
