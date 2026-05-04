using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A deterministic finite automaton for matching exact strings. Uses a sorted binary tree
    /// of state transitions to keep the memory footprint small while still allowing fast
    /// matches. Supports a single-character transition between states and an optional
    /// case-insensitive mode.
    /// </summary>
    internal class TokenStringDFA
    {

        private readonly DFAState[] _ascii = new DFAState[128];
        private readonly DFAState _nonAscii = new();

        /// <summary>
        /// Initializes a new <see cref="TokenStringDFA"/> with no registered patterns.
        /// </summary>
        public TokenStringDFA()
        {
        }

        /// <summary>
        /// Adds a string-to-pattern mapping to the DFA.
        /// </summary>
        /// <param name="str">The string to match.</param>
        /// <param name="caseInsensitive">Whether matching should be case-insensitive.</param>
        /// <param name="value">The token pattern accepted at the end of <paramref name="str"/>.</param>
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

        /// <summary>
        /// Attempts to match a registered string at the current position of
        /// <paramref name="buffer"/>, returning the longest match.
        /// </summary>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <param name="caseInsensitive">Whether matching should be case-insensitive.</param>
        /// <returns>The matched pattern, or <see langword="null"/> when no match was found.</returns>
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

        /// <summary>
        /// Returns a textual description of the registered DFA states and transitions.
        /// </summary>
        /// <returns>The textual description.</returns>
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
