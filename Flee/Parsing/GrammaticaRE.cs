namespace Flee.Parsing
{
    internal class GrammaticaRE : REHandler
    {
        private readonly RegExp _regExp;
        private Matcher _matcher = null;

        public GrammaticaRE(string regex, bool ignoreCase)
        {
            _regExp = new RegExp(regex, ignoreCase);
        }

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
