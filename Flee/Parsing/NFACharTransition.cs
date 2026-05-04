namespace Flee.Parsing
{
    /// <summary>
    /// A single-character match NFA transition.
    /// </summary>
    /// <param name="match">The character that triggers the transition.</param>
    /// <param name="state">The destination state.</param>
    internal class NFACharTransition(char match, NFAState state) : NFATransition(state)
    {
        private readonly char _match = match;

        /// <summary>
        /// Returns whether the matched character lies in the ASCII range.
        /// </summary>
        /// <returns><see langword="true"/> when the character is ASCII.</returns>
        public override bool IsAscii()
        {
            return _match is >= (char)0 and < (char)128;
        }

        /// <summary>
        /// Returns whether <paramref name="ch"/> equals the configured character.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> on a match.</returns>
        public override bool Match(char ch)
        {
            return _match == ch;
        }

        /// <summary>
        /// Creates a new char transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFACharTransition(_match, state);
        }
    }
}
