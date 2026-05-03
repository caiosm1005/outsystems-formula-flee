namespace Flee.Parsing
{
    /**
     * The dot ('.') character set transition. This transition
     * matches a single character that is not equal to a newline
     * character.
     */
    internal class NFADotTransition : NFATransition
    {
        public NFADotTransition(NFAState state) : base(state)
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
                case '\n':
                case '\r':
                case '\u0085':
                case '\u2028':
                case '\u2029':
                    return false;
                default:
                    return true;
            }
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFADotTransition(state);
        }
    }
}
