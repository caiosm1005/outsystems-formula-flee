namespace Flee.Parsing
{
    internal class RegExpMatcher(bool ignoreCase) : TokenMatcher(ignoreCase)
    {
        private REHandler[] _regExps = [];

        public override void AddPattern(TokenPattern pattern)
        {
            REHandler re;
            try
            {
                re = new GrammaticaRE(pattern.Pattern, IgnoreCase);
                pattern.DebugInfo = "Grammatica regexp\n" + re;
            }
            catch (Exception)
            {
                re = new SystemRE(pattern.Pattern, IgnoreCase);
                pattern.DebugInfo = "native .NET regexp";
            }
            Array.Resize(ref _regExps, _regExps.Length + 1);
            _regExps[_regExps.Length - 1] = re;
            base.AddPattern(pattern);
        }

        public override void Match(ReaderBuffer buffer, TokenMatch match)
        {
            for (int i = 0; i < _regExps.Length; i++)
            {
                int length = _regExps[i].Match(buffer);
                if (length > 0)
                {
                    match.Update(length, Patterns[i]);
                }
            }
        }
    }
}
