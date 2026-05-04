namespace Flee.Parsing
{
    /// <summary>
    /// The digit character set transition. This transition matches a single numeric character.
    /// </summary>
    internal class NFADigitTransition(NFAState state) : NFATransition(state)
    {
        /// <summary>
        /// Digit transitions only ever match ASCII characters.
        /// </summary>
        /// <returns>Always <see langword="true"/>.</returns>
        public override bool IsAscii()
        {
            return true;
        }

        /// <summary>
        /// Returns whether <paramref name="ch"/> is a decimal digit.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when <paramref name="ch"/> is in <c>0..9</c>.</returns>
        public override bool Match(char ch)
        {
            return ch is >= '0' and <= '9';
        }

        /// <summary>
        /// Creates a new digit transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFADigitTransition(state);
        }
    }
}
