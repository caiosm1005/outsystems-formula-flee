namespace Flee.Parsing
{
    /// <summary>
    /// A non-deterministic finite-state automaton (NFA) for matching tokens. Supports both
    /// fixed strings and simple regular expressions yet aims for DFA-class throughput thanks
    /// to highly optimized data structures and tuning.
    /// </summary>
    /// <remarks>
    /// The memory footprint during matching should be near zero — no heap memory is
    /// allocated unless the pre-allocated queues need to be enlarged. The NFA also avoids
    /// recursion in favor of an explicit loop.
    /// </remarks>
    internal class TokenNFA
    {
        private readonly NFAState[] _initialChar = new NFAState[128];
        private readonly NFAState _initial = new();
        private readonly NFAStateQueue _queue = new();

        /// <summary>
        /// Adds a fixed-string match starting at the implicit ASCII fast-path table when
        /// possible.
        /// </summary>
        /// <param name="str">The string to match.</param>
        /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
        /// <param name="value">The token pattern to associate with the match.</param>
        public void AddTextMatch(string str, bool ignoreCase, TokenPattern value)
        {
            NFAState state;
            char ch = str[0];

            if (ch < 128 && !ignoreCase)
            {
                state = _initialChar[ch];
                state ??= _initialChar[ch] = new NFAState();
            }
            else
            {
                state = _initial.AddOut(ch, ignoreCase, null);
            }
            for (int i = 1; i < str.Length; i++)
            {
                state = state.AddOut(str[i], ignoreCase, null);
            }
            state.Value = value;
        }

        /// <summary>
        /// Adds a regular-expression match by compiling <paramref name="pattern"/> into a
        /// sub-NFA and grafting it onto the initial state, choosing the most efficient entry
        /// strategy based on the pattern shape.
        /// </summary>
        /// <param name="pattern">The regex source.</param>
        /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
        /// <param name="value">The token pattern to associate with the match.</param>
        public void AddRegExpMatch(string pattern,
                                   bool ignoreCase,
                                   TokenPattern value)
        {
            TokenRegExpParser parser = new(pattern, ignoreCase);
            string debug = "DFA regexp; " + parser.GetDebugInfo();

            var isAscii = parser.Start.IsAsciiOutgoing();
            for (int i = 0; isAscii && i < 128; i++)
            {
                bool match = false;
                for (int j = 0; j < parser.Start.Outgoing.Length; j++)
                {
                    if (parser.Start.Outgoing[j].Match((char)i))
                    {
                        if (match)
                        {
                            isAscii = false;
                            break;
                        }
                        match = true;
                    }
                }
                if (match && _initialChar[i] != null)
                {
                    isAscii = false;
                }
            }
            if (parser.Start.Incoming.Length > 0)
            {
                _ = _initial.AddOut(new NFAEpsilonTransition(parser.Start));
                debug += ", uses initial epsilon";
            }
            else if (isAscii && !ignoreCase)
            {
                for (int i = 0; isAscii && i < 128; i++)
                {
                    for (int j = 0; j < parser.Start.Outgoing.Length; j++)
                    {
                        if (parser.Start.Outgoing[j].Match((char)i))
                        {
                            _initialChar[i] = parser.Start.Outgoing[j].State;
                        }
                    }
                }
                debug += ", uses ASCII lookup";
            }
            else
            {
                parser.Start.MergeInto(_initial);
                debug += ", uses initial state";
            }
            parser.End.Value = value;
            value.DebugInfo = debug;
        }

        /// <summary>
        /// Runs the NFA against <paramref name="buffer"/>, updating <paramref name="match"/>
        /// every time an accepting state is reached.
        /// </summary>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="match">The match accumulator to update.</param>
        /// <returns>Always zero — the actual match length is recorded on <paramref name="match"/>.</returns>
        public int Match(ReaderBuffer buffer, TokenMatch match)
        {
            int length = 0;
            int pos = 1;
            NFAState state;

            // The first step of the match loop has been unrolled and
            // optimized for performance below.
            _queue.Clear();
            var peekChar = buffer.Peek(0);
            if (peekChar is >= 0 and < 128)
            {
                state = _initialChar[peekChar];
                if (state != null)
                {
                    _queue.AddLast(state);
                }
            }
            if (peekChar >= 0)
            {
                _initial.MatchTransitions((char)peekChar, _queue, true);
            }
            _queue.MarkEnd();
            peekChar = buffer.Peek(1);

            // The remaining match loop processes all subsequent states
            while (!_queue.Empty)
            {
                if (_queue.Marked)
                {
                    pos++;
                    peekChar = buffer.Peek(pos);
                    _queue.MarkEnd();
                }
                state = _queue.RemoveFirst()!;
                if (state.Value != null)
                {
                    match.Update(pos, state.Value);
                }
                if (peekChar >= 0)
                {
                    state.MatchTransitions((char)peekChar, _queue, false);
                }
            }
            return length;
        }
    }
}
