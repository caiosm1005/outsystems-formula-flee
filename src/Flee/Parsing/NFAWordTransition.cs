namespace Flee.Parsing
{
    /// <summary>
    /// The word character set transition. This transition matches a single word
    /// character.
    /// </summary>
    internal class NFAWordTransition : NFATransition
    {

        public NFAWordTransition(NFAState state) : base(state)
        {
        }

        public override bool IsAscii()
        {
            return true;
        }

        
        public override bool Match(char ch)
        {
            return ('a' <= ch && ch <= 'z')
                || ('A' <= ch && ch <= 'Z')
                || ('0' <= ch && ch <= '9')
                || ch == '_';
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFAWordTransition(state);
        }
    }
}
