namespace Flee.Parsing
{
    /// <summary>
    /// The special epsilon transition. This transition matches the empty input, i.e. it is an
    /// automatic transition that doesn't read any input. As such, it returns false in the
    /// match method and is handled specially everywhere.
    /// </summary>
    internal class NFAEpsilonTransition(NFAState state) : NFATransition(state)
    {
        /// <summary>
        /// Epsilon transitions never report ASCII matchability.
        /// </summary>
        /// <returns>Always <see langword="false"/>.</returns>
        public override bool IsAscii()
        {
            return false;
        }

        /// <summary>
        /// Epsilon transitions don't consume input, so this never matches.
        /// </summary>
        /// <param name="ch">The candidate character (ignored).</param>
        /// <returns>Always <see langword="false"/>.</returns>
        public override bool Match(char ch)
        {
            return false;
        }

        /// <summary>
        /// Creates a new epsilon transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFAEpsilonTransition(state);
        }
    }
}
