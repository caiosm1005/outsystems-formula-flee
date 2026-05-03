namespace Flee.Parsing
{
    /**
     * The whitespace character set transition. This transition
     * matches a single whitespace character.
     */
    internal class NFAWhitespaceTransition : NFATransition
    {
        public NFAWhitespaceTransition(NFAState state) : base(state)
        {
        }

        public override bool IsAscii()
        {
            return true;
        }

        public override bool Match(char ch)
        {
            switch (ch)
            {
                case ' ':
                case '\t':
                case '\n':
                case '\f':
                case '\r':
                case (char)11:
                    return true;
                default:
                    return false;
            }
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFAWhitespaceTransition(state);
        }
    }
}
