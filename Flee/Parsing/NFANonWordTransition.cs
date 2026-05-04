namespace Flee.Parsing
{
    /**
     * The non-word character set transition. This transition matches
     * a single non-word character.
     */
    internal class NFANonWordTransition(NFAState state) : NFATransition(state)
    {
        public override bool IsAscii()
        {
            return false;
        }

        public override bool Match(char ch)
        {
            bool word = ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';
            return !word;
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFANonWordTransition(state);
        }
    }
}
