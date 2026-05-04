namespace Flee.Parsing
{
    internal class DFAState
    {

        internal TokenPattern? Value;

        internal TransitionTree Tree = new();
    }
}
