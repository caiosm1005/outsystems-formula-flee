namespace Flee.Parsing
{
    /**
     * An NFA state transition. A transition checks a single
     * character of input an determines if it is a match. If a match
     * is encountered, the NFA should move forward to the transition
     * state.
     */
    internal abstract class NFATransition
    {

        internal NFAState State;

        protected NFATransition(NFAState state)
        {
            State = state;
            State.AddIn(this);
        }

        public abstract bool IsAscii();

        public abstract bool Match(char ch);

        public abstract NFATransition Copy(NFAState state);
    }
}
