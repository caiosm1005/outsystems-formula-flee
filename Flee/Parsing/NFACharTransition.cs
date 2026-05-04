namespace Flee.Parsing
{
    /**
     * A single character match transition.
     */
    internal class NFACharTransition(char match, NFAState state) : NFATransition(state)
    {
        private readonly char _match = match;

        public override bool IsAscii()
        {
            return _match is >= (char)0 and < (char)128;
        }

        public override bool Match(char ch)
        {
            return _match == ch;
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFACharTransition(_match, state);
        }
    }
}
