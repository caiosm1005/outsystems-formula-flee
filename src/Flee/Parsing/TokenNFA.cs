namespace Flee.Parsing
{
    /// <summary>
    /// A non-deterministic finite state automaton (NFA) for matching tokens. It
    /// supports both fixed strings and simple regular expressions, but should
    /// perform similar to a DFA due to highly optimized data structures and
    /// tuning. The memory footprint during matching should be near zero, since
    /// no heap memory is allocated unless the pre-allocated queues need to be
    /// enlarged. The NFA also does not use recursion, but iterates in a loop
    /// instead.
    /// </summary>
    internal class TokenNFA
    {
        private readonly NFAState[] _initialChar = new NFAState[128];
        private readonly NFAState _initial = new NFAState();
        private readonly NFAStateQueue _queue = new NFAStateQueue();

        public void AddTextMatch(string str, bool ignoreCase, TokenPattern value)
        {
            NFAState state;
            char ch = str[0];

            if (ch < 128 && !ignoreCase)
            {
                state = _initialChar[ch];
                if (state == null)
                {
                    state = _initialChar[ch] = new NFAState();
                }
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

        public void AddRegExpMatch(string pattern,
                                   bool ignoreCase,
                                   TokenPattern value)
        {
            TokenRegExpParser parser = new TokenRegExpParser(pattern, ignoreCase);
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
                _initial.AddOut(new NFAEpsilonTransition(parser.Start));
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

        public int Match(ReaderBuffer buffer, TokenMatch match)
        {
            int length = 0;
            int pos = 1;
            NFAState state;

            // The first step of the match loop has been unrolled and
            // optimized for performance below.
            this._queue.Clear();
            var peekChar = buffer.Peek(0);
            if (0 <= peekChar && peekChar < 128)
            {
                state = this._initialChar[peekChar];
                if (state != null)
                {
                    this._queue.AddLast(state);
                }
            }
            if (peekChar >= 0)
            {
                this._initial.MatchTransitions((char)peekChar, this._queue, true);
            }
            this._queue.MarkEnd();
            peekChar = buffer.Peek(1);

            // The remaining match loop processes all subsequent states
            while (!this._queue.Empty)
            {
                if (this._queue.Marked)
                {
                    pos++;
                    peekChar = buffer.Peek(pos);
                    this._queue.MarkEnd();
                }
                state = this._queue.RemoveFirst();
                if (state.Value != null)
                {
                    match.Update(pos, state.Value);
                }
                if (peekChar >= 0)
                {
                    state.MatchTransitions((char)peekChar, this._queue, false);
                }
            }
            return length;
        }
    }
}
