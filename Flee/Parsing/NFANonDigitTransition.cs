namespace Flee.Parsing
{
    /**
     * The non-digit character set transition. This transition
     * matches a single non-numeric character.
     */
    internal class NFANonDigitTransition(NFAState state) : NFATransition(state)
    {
        public override bool IsAscii()
        {
            return false;
        }

        public override bool Match(char ch)
        {
            return ch is < '0' or > '9';
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFANonDigitTransition(state);
        }
    }
}
