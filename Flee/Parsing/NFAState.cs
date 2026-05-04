namespace Flee.Parsing
{
    /// <summary>
    /// A single state in a non-deterministic finite automaton. Each state holds zero or more
    /// incoming and outgoing transitions plus an optional accepted-token value.
    /// </summary>
    internal class NFAState
    {
        /// <summary>
        /// The token pattern accepted at this state, or <see langword="null"/> if it is not
        /// accepting.
        /// </summary>
        internal TokenPattern? Value = null;

        /// <summary>
        /// The transitions that lead into this state.
        /// </summary>
        internal NFATransition[] Incoming = [];

        /// <summary>
        /// The transitions that leave this state.
        /// </summary>
        internal NFATransition[] Outgoing = [];

        /// <summary>
        /// Whether this state has at least one outgoing epsilon transition (precomputed for
        /// performance).
        /// </summary>
        internal bool EpsilonOut = false;

        /// <summary>
        /// Returns whether this state has any transitions.
        /// </summary>
        /// <returns><see langword="true"/> when at least one incoming or outgoing transition exists.</returns>
        public bool HasTransitions()
        {
            return Incoming.Length > 0 || Outgoing.Length > 0;
        }

        /// <summary>
        /// Returns whether every outgoing transition is restricted to ASCII characters.
        /// </summary>
        /// <returns><see langword="true"/> when every outgoing transition is ASCII-only.</returns>
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

        /// <summary>
        /// Adds <paramref name="trans"/> to the incoming-transition list.
        /// </summary>
        /// <param name="trans">The transition to add.</param>
        public void AddIn(NFATransition trans)
        {
            Array.Resize(ref Incoming, Incoming.Length + 1);
            Incoming[Incoming.Length - 1] = trans;
        }

        /// <summary>
        /// Adds an outgoing character transition keyed by <paramref name="ch"/>, optionally
        /// reusing an existing equivalent transition.
        /// </summary>
        /// <param name="ch">The character that triggers the transition.</param>
        /// <param name="ignoreCase">Whether to treat upper- and lower-case as equivalent.</param>
        /// <param name="state">The destination state, or <see langword="null"/> to create a new one.</param>
        /// <returns>The destination state actually used.</returns>
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

        /// <summary>
        /// Adds <paramref name="trans"/> to the outgoing-transition list and returns its
        /// destination state.
        /// </summary>
        /// <param name="trans">The transition to add.</param>
        /// <returns>The destination state of <paramref name="trans"/>.</returns>
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

        /// <summary>
        /// Merges this state into <paramref name="state"/>, redirecting all incoming and
        /// outgoing transitions.
        /// </summary>
        /// <param name="state">The state that absorbs this one.</param>
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

        /// <summary>
        /// Enqueues every successor state reachable from this one by consuming
        /// <paramref name="ch"/>.
        /// </summary>
        /// <param name="ch">The next input character.</param>
        /// <param name="queue">The queue to add successor states to.</param>
        /// <param name="initial">Whether this is the initial-state expansion.</param>
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

        /// <summary>
        /// Enqueues every successor state reachable from this one through epsilon transitions.
        /// </summary>
        /// <param name="queue">The queue to add successor states to.</param>
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
