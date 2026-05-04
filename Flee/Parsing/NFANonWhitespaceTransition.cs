namespace Flee.Parsing
{
    /// <summary>
    /// The non-whitespace character set transition. This transition matches a single
    /// non-whitespace character.
    /// </summary>
    internal class NFANonWhitespaceTransition(NFAState state) : NFATransition(state)
    {
        /// <summary>
        /// The match set spans the full Unicode range, so this transition isn't ASCII-only.
        /// </summary>
        /// <returns>Always <see langword="false"/>.</returns>
        public override bool IsAscii()
        {
            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="ch"/> is anything other than a whitespace character.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when <paramref name="ch"/> is non-whitespace.</returns>
        public override bool Match(char ch)
        {
            return ch switch
            {
                ' ' or '\t' or '\n' or '\f' or '\r' or (char)11 => false,
                _ => true,
            };
        }

        /// <summary>
        /// Creates a new non-whitespace transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFANonWhitespaceTransition(state);
        }
    }
}
