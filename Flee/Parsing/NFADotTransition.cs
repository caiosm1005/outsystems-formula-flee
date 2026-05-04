namespace Flee.Parsing
{
    /**
     * The dot ('.') character set transition. This transition
     * matches a single character that is not equal to a newline
     * character.
     */
    internal class NFADotTransition(NFAState state) : NFATransition(state)
    {
        public override bool IsAscii()
        {
            return false;
        }

        public override bool Match(char ch)
        {
            return ch switch
            {
                '\n' or '\r' or '\u0085' or '\u2028' or '\u2029' => false,
                _ => true,
            };
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFADotTransition(state);
        }
    }
}
