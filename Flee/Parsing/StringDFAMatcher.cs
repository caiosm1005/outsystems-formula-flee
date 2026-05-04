namespace Flee.Parsing
{
    internal class StringDFAMatcher(bool ignoreCase) : TokenMatcher(ignoreCase)
    {

        private readonly TokenStringDFA _automaton = new();

        public override void AddPattern(TokenPattern pattern)
        {
            _automaton.AddMatch(pattern.Pattern, IgnoreCase, pattern);
            base.AddPattern(pattern);
        }

        public override void Match(ReaderBuffer buffer, TokenMatch match)
        {
            TokenPattern? res = _automaton.Match(buffer, IgnoreCase);

            if (res != null)
            {
                match.Update(res.Pattern.Length, res);
            }
        }
    }
}
