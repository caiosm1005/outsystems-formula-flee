using System.Text;

namespace Flee.Parsing
{
    internal class TransitionTree
    {
        private char _value = '\0';
        private DFAState? _state;
        private TransitionTree? _left;
        private TransitionTree? _right;

        public TransitionTree()
        {
        }

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
