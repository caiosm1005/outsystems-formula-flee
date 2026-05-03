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
            if (_value == '\0' || _value == c)
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

        public void Add(char c, bool lowerCase, DFAState state)
        {
            if (lowerCase)
            {
                c = Char.ToLower(c);
            }
            if (_value == '\0')
            {
                this._value = c;
                this._state = state;
                this._left = new TransitionTree();
                this._right = new TransitionTree();
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
            if (this._value != '\0')
            {
                if (buffer.Length > 0 && buffer[buffer.Length - 1] == '\n')
                {
                    buffer.Append(indent);
                }
                buffer.Append(this._value);
                if (this._state!.Value != null)
                {
                    buffer.Append(": ");
                    buffer.Append(this._state.Value);
                    buffer.Append("\n");
                }
                this._state.Tree.PrintTo(buffer, indent + " ");
            }
            _right?.PrintTo(buffer, indent);
        }
    }
}
