namespace Flee.Parsing
{
    /**
     * An NFA state. The NFA consists of a series of states, each
     * having zero or more transitions to other states.
     */
    internal class NFAState
    {
        internal TokenPattern? Value = null;
        internal NFATransition[] Incoming = [];
        internal NFATransition[] Outgoing = [];
        internal bool EpsilonOut = false;

        public bool HasTransitions()
        {
            return Incoming.Length > 0 || Outgoing.Length > 0;
        }
        public bool IsAsciiOutgoing()
        {
            for (int i = 0; i < Outgoing.Length; i++)
            {
                if (!Outgoing[i].IsAscii())
                {
                    return false;
                }
            }
            return true;
        }

        public void AddIn(NFATransition trans)
        {
            Array.Resize(ref Incoming, Incoming.Length + 1);
            Incoming[Incoming.Length - 1] = trans;
        }

        public NFAState AddOut(char ch, bool ignoreCase, NFAState? state)
        {
            if (ignoreCase)
            {
                state ??= new NFAState();
                _ = AddOut(new NFACharTransition(Char.ToLower(ch), state));
                _ = AddOut(new NFACharTransition(Char.ToUpper(ch), state));
                return state;
            }
            else
            {
                if (state == null)
                {
                    state = FindUniqueCharTransition(ch);
                    if (state != null)
                    {
                        return state;
                    }
                    state = new NFAState();
                }
                return AddOut(new NFACharTransition(ch, state));
            }
        }

        public NFAState AddOut(NFATransition trans)
        {
            Array.Resize(ref Outgoing, Outgoing.Length + 1);
            Outgoing[Outgoing.Length - 1] = trans;
            if (trans is NFAEpsilonTransition)
            {
                EpsilonOut = true;
            }
            return trans.State;
        }

        public void MergeInto(NFAState state)
        {
            for (int i = 0; i < Incoming.Length; i++)
            {
                state.AddIn(Incoming[i]);
                Incoming[i].State = state;
            }
            Incoming = null!;
            for (int i = 0; i < Outgoing.Length; i++)
            {
                _ = state.AddOut(Outgoing[i]);
            }
            Outgoing = null!;
        }

        private NFAState? FindUniqueCharTransition(char ch)
        {
            NFATransition? res = null;
            NFATransition trans;

            for (int i = 0; i < Outgoing.Length; i++)
            {
                trans = Outgoing[i];
                if (trans.Match(ch) && trans is NFACharTransition)
                {
                    if (res != null)
                    {
                        return null;
                    }
                    res = trans;
                }
            }
            for (int i = 0; res != null && i < Outgoing.Length; i++)
            {
                trans = Outgoing[i];
                if (trans != res && trans.State == res.State)
                {
                    return null;
                }
            }
            return res?.State;
        }

        public void MatchTransitions(char ch, NFAStateQueue queue, bool initial)
        {
            for (int i = 0; i < Outgoing.Length; i++)
            {
                var trans = Outgoing[i];
                var target = trans.State;
                if (initial && trans is NFAEpsilonTransition)
                {
                    target.MatchTransitions(ch, queue, true);
                }
                else if (trans.Match(ch))
                {
                    queue.AddLast(target);
                    if (target.EpsilonOut)
                    {
                        target.MatchEmpty(queue);
                    }
                }
            }
        }

        public void MatchEmpty(NFAStateQueue queue)
        {
            for (int i = 0; i < Outgoing.Length; i++)
            {
                var trans = Outgoing[i];
                if (trans is NFAEpsilonTransition)
                {
                    var target = trans.State;
                    queue.AddLast(target);
                    if (target.EpsilonOut)
                    {
                        target.MatchEmpty(queue);
                    }
                }
            }
        }
    }
}
