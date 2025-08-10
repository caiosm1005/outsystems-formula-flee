namespace Flee.Parsing
{
    internal class NFAMatcher : TokenMatcher
    {

        private readonly TokenNFA _automaton = new TokenNFA();

        public NFAMatcher(bool ignoreCase) : base(ignoreCase)
        {
        }

        public override void AddPattern(TokenPattern pattern)
        {
            if (pattern.Type == TokenPattern.PatternType.STRING)
            {
                _automaton.AddTextMatch(pattern.Pattern, IgnoreCase, pattern);
            }
            else
            {
                _automaton.AddRegExpMatch(pattern.Pattern, IgnoreCase, pattern);
            }
            base.AddPattern(pattern);
        }

        public override void Match(ReaderBuffer buffer, TokenMatch match)
        {
            _automaton.Match(buffer, match);
        }
    }
}
