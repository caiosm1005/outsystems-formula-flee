namespace Flee.Parsing
{
    /// <summary>
    /// The word character set transition. This transition matches a single word character
    /// (letters, digits, or underscore).
    /// </summary>
    internal class NFAWordTransition(NFAState state) : NFATransition(state)
    {
        /// <summary>
        /// The recognized word set is ASCII-only.
        /// </summary>
        /// <returns>Always <see langword="true"/>.</returns>
        public override bool IsAscii()
        {
            return true;
        }

        /// <summary>
        /// Returns whether <paramref name="ch"/> is a word character.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when <paramref name="ch"/> is a letter, digit, or underscore.</returns>
        public override bool Match(char ch)
        {
            return ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';
        }

        /// <summary>
        /// Creates a new word transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFAWordTransition(state);
        }
    }
}
