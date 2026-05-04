namespace Flee.Parsing
{
    /// <summary>
    /// <see cref="TokenMatcher"/> implementation backed by a deterministic finite automaton
    /// for fixed-string tokens.
    /// </summary>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    internal class StringDFAMatcher(bool ignoreCase) : TokenMatcher(ignoreCase)
    {

        private readonly TokenStringDFA _automaton = new();

        /// <summary>
        /// Adds <paramref name="pattern"/> to the underlying DFA.
        /// </summary>
        /// <param name="pattern">The token pattern to add.</param>
        public override void AddPattern(TokenPattern pattern)
        {
            _automaton.AddMatch(pattern.Pattern, IgnoreCase, pattern);
            base.AddPattern(pattern);
        }

        /// <summary>
        /// Runs the DFA against <paramref name="buffer"/>, updating <paramref name="match"/>
        /// when a match is found.
        /// </summary>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="match">The match accumulator to update.</param>
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
