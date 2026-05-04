namespace Flee.Parsing
{
    /**
     * The whitespace character set transition. This transition
     * matches a single whitespace character.
     */
    internal class NFAWhitespaceTransition(NFAState state) : NFATransition(state)
    {
        public override bool IsAscii()
        {
            return true;
        }

        public override bool Match(char ch)
        {
            return ch switch
            {
                ' ' or '\t' or '\n' or '\f' or '\r' or (char)11 => true,
                _ => false,
            };
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFAWhitespaceTransition(state);
        }
    }
}
