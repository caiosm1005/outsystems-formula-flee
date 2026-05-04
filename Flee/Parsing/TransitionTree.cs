using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A binary search tree of DFA transitions. Each node carries a single triggering
    /// character plus the destination <see cref="DFAState"/>.
    /// </summary>
    internal class TransitionTree
    {
        private char _value = '\0';
        private DFAState? _state;
        private TransitionTree? _left;
        private TransitionTree? _right;

        /// <summary>
        /// Initializes a new, empty <see cref="TransitionTree"/>.
        /// </summary>
        public TransitionTree()
        {
        }

        /// <summary>
        /// Looks up the destination state for the transition keyed by <paramref name="c"/>.
        /// </summary>
        /// <param name="c">The character to match.</param>
        /// <param name="lowerCase">Whether to lower-case <paramref name="c"/> before matching.</param>
        /// <returns>The destination state, or <see langword="null"/> when no transition matches.</returns>
        public DFAState? Find(char c, bool lowerCase)
        {
            if (lowerCase)
            {
                c = Char.ToLower(c);
            }
            return _value == '\0' || _value == c
                ? _state
                : _value > c ? _left!.Find(c, false) : _right!.Find(c, false);
        }

        /// <summary>
        /// Adds a transition keyed by <paramref name="c"/> pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="c">The character that triggers the transition.</param>
        /// <param name="lowerCase">Whether to lower-case <paramref name="c"/> before adding.</param>
        /// <param name="state">The destination state.</param>
        public void Add(char c, bool lowerCase, DFAState state)
        {
            if (lowerCase)
            {
                c = Char.ToLower(c);
            }
            if (_value == '\0')
            {
                _value = c;
                _state = state;
                _left = new TransitionTree();
                _right = new TransitionTree();
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

        /// <summary>
        /// Writes a textual description of this subtree to <paramref name="buffer"/>, used by
        /// <see cref="TokenStringDFA.ToString"/>.
        /// </summary>
        /// <param name="buffer">The string builder to append to.</param>
        /// <param name="indent">The indentation prefix to apply to each line.</param>
        public void PrintTo(StringBuilder buffer, String indent)
        {
            _left?.PrintTo(buffer, indent);
            if (_value != '\0')
            {
                if (buffer.Length > 0 && buffer[buffer.Length - 1] == '\n')
                {
                    _ = buffer.Append(indent);
                }
                _ = buffer.Append(_value);
                if (_state!.Value != null)
                {
                    _ = buffer.Append(": ");
                    _ = buffer.Append(_state.Value);
                    _ = buffer.Append("\n");
                }
                _state.Tree.PrintTo(buffer, indent + " ");
            }
            _right?.PrintTo(buffer, indent);
        }
    }
}
