namespace Flee.Parsing
{
    /// <summary>
    /// A simple character-by-character matching automaton used by the string-DFA tokenizer to
    /// recognize fixed strings. Each state holds an optional value (the matched object) and a
    /// transition tree keyed by single characters.
    /// </summary>
    internal class Automaton
    {
        private object? _value;
        private readonly AutomatonTree _tree = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Automaton"/> class.
        /// </summary>
        public Automaton()
        {
        }

        /// <summary>
        /// Adds a string-to-value mapping to the automaton, expanding the underlying state
        /// machine as needed.
        /// </summary>
        /// <param name="str">The string to match.</param>
        /// <param name="caseInsensitive">Whether the match should ignore case.</param>
        /// <param name="value">The value to return when the string is matched.</param>
        public void AddMatch(string str, bool caseInsensitive, object value)
        {
            if (str.Length == 0)
            {
                _value = value;
            }
            else
            {
                var state = _tree.Find(str[0], caseInsensitive);
                if (state == null)
                {
                    state = new Automaton();
                    state.AddMatch(str.Substring(1), caseInsensitive, value);
                    _tree.Add(str[0], caseInsensitive, state);
                }
                else
                {
                    state.AddMatch(str.Substring(1), caseInsensitive, value);
                }
            }
        }

        /// <summary>
        /// Attempts to match a previously registered string starting at <paramref name="pos"/>
        /// in <paramref name="input"/>. Returns the value associated with the longest match.
        /// </summary>
        /// <param name="input">The look-ahead reader to consume characters from.</param>
        /// <param name="pos">The starting offset relative to the reader's current position.</param>
        /// <param name="caseInsensitive">Whether the match should ignore case.</param>
        /// <returns>The matched value, or <see langword="null"/> if no match was found.</returns>
        public object? MatchFrom(LookAheadReader input, int pos, bool caseInsensitive)
        {

            object? result = null;
            int c = input.Peek(pos);
            if (_tree != null && c >= 0)
            {
                Automaton? state = _tree.Find(Convert.ToChar(c), caseInsensitive);
                if (state != null)
                {
                    result = state.MatchFrom(input, pos + 1, caseInsensitive);
                }
            }
            return result ?? _value;
        }
    }
}
