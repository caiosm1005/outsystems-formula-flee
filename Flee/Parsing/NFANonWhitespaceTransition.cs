namespace Flee.Parsing
{
    /**
     * The non-whitespace character set transition. This transition
     * matches a single non-whitespace character.
     */
    internal class NFANonWhitespaceTransition(NFAState state) : NFATransition(state)
    {
        public override bool IsAscii()
        {
            return false;
        }

        public override bool Match(char ch)
        {
            return ch switch
            {
                ' ' or '\t' or '\n' or '\f' or '\r' or (char)11 => false,
                _ => true,
            };
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFANonWhitespaceTransition(state);
        }
    }
}
