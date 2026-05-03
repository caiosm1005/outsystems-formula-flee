namespace Flee.Parsing
{
    /**
     * The digit character set transition. This transition matches a
     * single numeric character.
     */
    internal class NFADigitTransition : NFATransition
    {
        public NFADigitTransition(NFAState state) : base(state)
        {
        }

        public override bool IsAscii()
        {
            return true;
        }

        public override bool Match(char ch)
        {
            return '0' <= ch && ch <= '9';
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFADigitTransition(state);
        }
    }
}
