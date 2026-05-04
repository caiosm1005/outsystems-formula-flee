namespace Flee.Parsing
{
    /// <summary>
    /// Common base class for NFA transitions. Each transition checks a single input character
    /// and, if it matches, signals that the NFA should advance to the destination state.
    /// </summary>
    internal abstract class NFATransition
    {
        /// <summary>
        /// The destination state of this transition.
        /// </summary>
        internal NFAState State;

        /// <summary>
        /// Initializes a new transition pointing at <paramref name="state"/>, registering this
        /// transition as one of the state's incoming transitions.
        /// </summary>
        /// <param name="state">The destination state.</param>
        protected NFATransition(NFAState state)
        {
            State = state;
            State.AddIn(this);
        }

        /// <summary>
        /// Returns whether every character that can trigger this transition lies in the ASCII
        /// range.
        /// </summary>
        /// <returns><see langword="true"/> when the match set is ASCII-only.</returns>
        public abstract bool IsAscii();

        /// <summary>
        /// Returns whether <paramref name="ch"/> triggers this transition.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when the transition matches.</returns>
        public abstract bool Match(char ch);

        /// <summary>
        /// Creates a copy of this transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public abstract NFATransition Copy(NFAState state);
    }
}
