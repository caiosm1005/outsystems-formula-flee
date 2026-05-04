namespace Flee.Parsing
{
    /**
     * The digit character set transition. This transition matches a
     * single numeric character.
     */
    internal class NFADigitTransition(NFAState state) : NFATransition(state)
    {
        public override bool IsAscii()
        {
            return true;
        }

        public override bool Match(char ch)
        {
            return ch is >= '0' and <= '9';
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFADigitTransition(state);
        }
    }
}
