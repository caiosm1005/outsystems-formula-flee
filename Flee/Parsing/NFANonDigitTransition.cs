namespace Flee.Parsing
{
    /// <summary>
    /// The non-digit character set transition. This transition matches a single non-numeric character.
    /// </summary>
    internal class NFANonDigitTransition(NFAState state) : NFATransition(state)
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
        /// Returns whether <paramref name="ch"/> is anything other than a decimal digit.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when <paramref name="ch"/> is not in <c>0..9</c>.</returns>
        public override bool Match(char ch)
        {
            return ch is < '0' or > '9';
        }

        /// <summary>
        /// Creates a new non-digit transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFANonDigitTransition(state);
        }
    }
}
