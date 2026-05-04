namespace Flee.Parsing
{
    /// <summary>
    /// The non-word character set transition. This transition matches a single non-word character.
    /// </summary>
    internal class NFANonWordTransition(NFAState state) : NFATransition(state)
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
        /// Returns whether <paramref name="ch"/> is anything other than a word character.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="ch"/> is not a letter, digit, or underscore.
        /// </returns>
        public override bool Match(char ch)
        {
            bool word = ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';
            return !word;
        }

        /// <summary>
        /// Creates a new non-word transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFANonWordTransition(state);
        }
    }
}
