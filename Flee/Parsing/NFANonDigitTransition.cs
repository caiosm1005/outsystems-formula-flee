namespace Flee.Parsing
{
    /**
     * The non-digit character set transition. This transition
     * matches a single non-numeric character.
     */
    internal class NFANonDigitTransition : NFATransition
    {
        public NFANonDigitTransition(NFAState state) : base(state)
        {
        }

        public override bool IsAscii()
        {
            return false;
        }

        public override bool Match(char ch)
        {
            return ch < '0' || '9' < ch;
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFANonDigitTransition(state);
        }
    }
}
