namespace Flee.Parsing
{
    /**
     * The non-whitespace character set transition. This transition
     * matches a single non-whitespace character.
     */
    internal class NFANonWhitespaceTransition : NFATransition
    {

        public NFANonWhitespaceTransition(NFAState state) : base(state)
        {
        }

        public override bool IsAscii()
        {
            return false;
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
                    return false;
                default:
                    return true;
            }
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFANonWhitespaceTransition(state);
        }
    }
}
