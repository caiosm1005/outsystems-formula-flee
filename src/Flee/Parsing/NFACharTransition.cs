namespace Flee.Parsing
{
    /// <summary>
    /// A single character match transition.
    /// </summary>
    internal class NFACharTransition : NFATransition
    {
        private readonly char _match;

        public NFACharTransition(char match, NFAState state) : base(state)
        {
            _match = match;
        }

        public override bool IsAscii()
        {
            return 0 <= _match && _match < 128;
        }

        public override bool Match(char ch)
        {
            return this._match == ch;
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFACharTransition(_match, state);
        }
    }
}
