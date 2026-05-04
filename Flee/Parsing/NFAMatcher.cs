namespace Flee.Parsing
{
    /// <summary>
    /// <see cref="TokenMatcher"/> implementation backed by an NFA. Forwards both fixed-string
    /// and regular-expression patterns to the same underlying <see cref="TokenNFA"/>.
    /// </summary>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    internal class NFAMatcher(bool ignoreCase) : TokenMatcher(ignoreCase)
    {

        private readonly TokenNFA _automaton = new();

        /// <summary>
        /// Adds <paramref name="pattern"/> to the underlying NFA, dispatching on the pattern
        /// type.
        /// </summary>
        /// <param name="pattern">The token pattern to add.</param>
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

        /// <summary>
        /// Runs the NFA against <paramref name="buffer"/>, updating <paramref name="match"/>
        /// when a longer match is found.
        /// </summary>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="match">The match accumulator to update.</param>
        public override void Match(ReaderBuffer buffer, TokenMatch match)
        {
            _ = _automaton.Match(buffer, match);
        }
    }
}
