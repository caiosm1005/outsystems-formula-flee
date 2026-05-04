namespace Flee.Parsing
{
    /// <summary>
    /// A single state in the deterministic-finite-state-automaton used by
    /// <see cref="TokenStringDFA"/>. Stores the matched <see cref="TokenPattern"/> (if any)
    /// and a transition tree to the successor states.
    /// </summary>
    internal class DFAState
    {
        /// <summary>
        /// The token pattern accepted at this state, or <see langword="null"/> if the state
        /// is not accepting.
        /// </summary>
        internal TokenPattern? Value;

        /// <summary>
        /// The transition tree to successor states, keyed by the next character in the input.
        /// </summary>
        internal TransitionTree Tree = new();
    }
}
