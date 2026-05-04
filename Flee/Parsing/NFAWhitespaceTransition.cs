namespace Flee.Parsing
{
    /// <summary>
    /// The whitespace character set transition. This transition matches a single whitespace
    /// character (space, tab, newline, form feed, carriage return, or vertical tab).
    /// </summary>
    internal class NFAWhitespaceTransition(NFAState state) : NFATransition(state)
    {
        /// <summary>
        /// The recognized whitespace set is ASCII-only.
        /// </summary>
        /// <returns>Always <see langword="true"/>.</returns>
        public override bool IsAscii()
        {
            return true;
        }

        /// <summary>
        /// Returns whether <paramref name="ch"/> is one of the recognized whitespace characters.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when <paramref name="ch"/> is whitespace.</returns>
        public override bool Match(char ch)
        {
            return ch switch
            {
                ' ' or '\t' or '\n' or '\f' or '\r' or (char)11 => true,
                _ => false,
            };
        }

        /// <summary>
        /// Creates a new whitespace transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFAWhitespaceTransition(state);
        }
    }
}
