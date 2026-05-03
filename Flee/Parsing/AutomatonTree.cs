namespace Flee.Parsing
{
    // * An automaton state transition tree. This class contains a
    // * binary search tree for the automaton transitions from one state
    // * to another. All transitions are linked to a single character.
    internal class AutomatonTree
    {
        private char _value;
        private Automaton? _state;
        private AutomatonTree? _left;
        private AutomatonTree? _right;

        public AutomatonTree()
        {
        }

        public Automaton? Find(char c, bool lowerCase)
        {
            if (lowerCase)
            {
                c = Char.ToLower(c);
            }
            if (_value == (char)0 || _value == c)
            {
                return _state;
            }
            else if (_value > c)
            {
                return _left!.Find(c, false);
            }
            else
            {
                return _right!.Find(c, false);
            }
        }

        public void Add(char c, bool lowerCase, Automaton state)
        {
            if (lowerCase)
            {
                c = Char.ToLower(c);
            }
            if (_value == (char)0)
            {
                this._value = c;
                this._state = state;
                this._left = new AutomatonTree();
                this._right = new AutomatonTree();
            }
            else if (_value > c)
            {
                _left!.Add(c, false, state);
            }
            else
            {
                _right!.Add(c, false, state);
            }
        }
    }
}
