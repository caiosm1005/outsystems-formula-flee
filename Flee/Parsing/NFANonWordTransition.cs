namespace Flee.Parsing
{
    /**
     * The non-word character set transition. This transition matches
     * a single non-word character.
     */
    internal class NFANonWordTransition : NFATransition
    {
        public NFANonWordTransition(NFAState state) : base(state)
        {
        }

        public override bool IsAscii()
        {
            return false;
        }

        public override bool Match(char ch)
        {
            bool word = ('a' <= ch && ch <= 'z')
                     || ('A' <= ch && ch <= 'Z')
                     || ('0' <= ch && ch <= '9')
                     || ch == '_';
            return !word;
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFANonWordTransition(state);
        }
    }
}
