using System.Text;

namespace Flee.Parsing
{
    /**
     * A deterministic finite state automaton for matching exact strings.
     * It uses a sorted binary tree representation of the state
     * transitions in order to enable quick matches with a minimal memory
     * footprint. It only supports a single character transition between
     * states, but may be run in an all case-insensitive mode.
     */
    internal class TokenStringDFA
    {

        private readonly DFAState[] _ascii = new DFAState[128];
        private readonly DFAState _nonAscii = new();

        public TokenStringDFA()
        {
        }

        public void AddMatch(string str, bool caseInsensitive, TokenPattern value)
        {
            DFAState state;
            char c = str[0];
            int start = 0;

            if (caseInsensitive)
            {
                c = Char.ToLower(c);
            }
            if (c < 128)
            {
                state = _ascii[c];
                state ??= _ascii[c] = new DFAState();
                start++;
            }
            else
            {
                state = _nonAscii;
            }
            for (int i = start; i < str.Length; i++)
            {
                var next = state.Tree.Find(str[i], caseInsensitive);
                if (next == null)
                {
                    next = new DFAState();
                    state.Tree.Add(str[i], caseInsensitive, next);
                }
                state = next;
            }
            state.Value = value;
        }

        public TokenPattern? Match(ReaderBuffer buffer, bool caseInsensitive)
        {
            TokenPattern? result = null;
            DFAState? state;
            int pos = 0;

            var c = buffer.Peek(0);
            if (c < 0)
            {
                return null;
            }
            if (caseInsensitive)
            {
                c = Char.ToLower((char)c);
            }
            if (c < 128)
            {
                state = _ascii[c];
                if (state == null)
                {
                    return null;
                }
                else if (state.Value != null)
                {
                    result = state.Value;
                }
                pos++;
            }
            else
            {
                state = _nonAscii;
            }
            while ((c = buffer.Peek(pos)) >= 0)
            {
                state = state.Tree.Find((char)c, caseInsensitive);
                if (state == null)
                {
                    break;
                }
                else if (state.Value != null)
                {
                    result = state.Value;
                }
                pos++;
            }
            return result;
        }

        public override string ToString()
        {
            StringBuilder buffer = new();

            for (int i = 0; i < _ascii.Length; i++)
            {
                if (_ascii[i] != null)
                {
                    _ = buffer.Append((char)i);
                    if (_ascii[i].Value != null)
                    {
                        _ = buffer.Append(": ");
                        _ = buffer.Append(_ascii[i].Value);
                        _ = buffer.Append("\n");
                    }
                    _ascii[i].Tree.PrintTo(buffer, " ");
                }
            }
            _nonAscii.Tree.PrintTo(buffer, "");
            return buffer.ToString();
        }
    }
}
