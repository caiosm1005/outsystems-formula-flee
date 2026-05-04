namespace Flee.Parsing
{
    /// <summary>
    /// An automaton state-transition tree. Holds a binary search tree of automaton transitions
    /// from one state to another. All transitions are keyed by a single character.
    /// </summary>
    internal class AutomatonTree
    {
        private char _value;
        private Automaton? _state;
        private AutomatonTree? _left;
        private AutomatonTree? _right;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomatonTree"/> class.
        /// </summary>
        public AutomatonTree()
        {
        }

        /// <summary>
        /// Looks up the destination state for the transition keyed by <paramref name="c"/>.
        /// </summary>
        /// <param name="c">The character to match.</param>
        /// <param name="lowerCase">Whether to lower-case <paramref name="c"/> before matching.</param>
        /// <returns>The destination automaton, or <see langword="null"/> when no transition matches.</returns>
        public Automaton? Find(char c, bool lowerCase)
        {
            if (lowerCase)
            {
                c = Char.ToLower(c);
            }
            return _value == (char)0 || _value == c
                ? _state
                : _value > c ? _left!.Find(c, false) : _right!.Find(c, false);
        }

        /// <summary>
        /// Adds a transition keyed by <paramref name="c"/> to <paramref name="state"/>, growing
        /// the binary search tree as needed.
        /// </summary>
        /// <param name="c">The character that triggers the transition.</param>
        /// <param name="lowerCase">Whether to lower-case <paramref name="c"/> before adding.</param>
        /// <param name="state">The destination automaton.</param>
        public void Add(char c, bool lowerCase, Automaton state)
        {
            if (lowerCase)
            {
                c = Char.ToLower(c);
            }
            if (_value == (char)0)
            {
                _value = c;
                _state = state;
                _left = new AutomatonTree();
                _right = new AutomatonTree();
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
