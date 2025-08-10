namespace Flee.Parsing
{
    /// <summary>
    /// The special epsilon transition. This transition matches the empty input,
    /// i.e. it is an automatic transition that doesn't read any input. As such,
    /// it returns false in the match method and is handled specially
    /// everywhere.
    /// </summary>
    internal class NFAEpsilonTransition : NFATransition
    {
        public NFAEpsilonTransition(NFAState state) : base(state)
        {
        }

        public override bool IsAscii()
        {
            return false;
        }

        public override bool Match(char ch)
        {
            return false;
        }

        public override NFATransition Copy(NFAState state)
        {
            return new NFAEpsilonTransition(state);
        }
    }
}
