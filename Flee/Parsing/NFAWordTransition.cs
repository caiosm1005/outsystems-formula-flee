namespace Flee.Parsing
{
    /**
     * The word character set transition. This transition matches a
     * single word character.
     */
    internal class NFAWordTransition(NFAState state) : NFATransition(state)
    {
        public override bool IsAscii()
        {
            return true;
        }


        public override bool Match(char ch)
        {
            return ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFAWordTransition(state);
        }
    }
}
