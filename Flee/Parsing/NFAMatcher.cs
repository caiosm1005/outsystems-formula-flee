namespace Flee.Parsing
{
    internal class NFAMatcher(bool ignoreCase) : TokenMatcher(ignoreCase)
    {

        private readonly TokenNFA _automaton = new();

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
            _ = _automaton.Match(buffer, match);
        }
    }
}
