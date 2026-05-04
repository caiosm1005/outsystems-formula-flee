namespace Flee.Parsing
{
    internal class GrammaticaRE(string regex, bool ignoreCase) : REHandler
    {
        private readonly RegExp _regExp = new(regex, ignoreCase);
        private Matcher? _matcher = null;

        public override int Match(ReaderBuffer buffer)
        {
            if (_matcher == null)
            {
                _matcher = _regExp.Matcher(buffer);
            }
            else
            {
                _matcher.Reset(buffer);
            }
            return _matcher.MatchFromBeginning() ? _matcher.Length() : 0;
        }
    }
}
