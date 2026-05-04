namespace Flee.Parsing
{
    /// <summary>
    /// A character-range NFA transition. Used for user-defined character sets in regular
    /// expressions; the set may be inverted and may match either single characters or ranges.
    /// </summary>
    /// <param name="inverse">Whether the membership test should be inverted.</param>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    /// <param name="state">The destination state.</param>
    internal class NFACharRangeTransition(bool inverse,
                                  bool ignoreCase,
                                  NFAState state) : NFATransition(state)
    {
        /// <summary>
        /// Whether the membership test is inverted.
        /// </summary>
        protected bool Inverse = inverse;

        /// <summary>
        /// Whether matching is case-insensitive.
        /// </summary>
        protected bool IgnoreCase = ignoreCase;

        private object[] _contents = [];

        /// <summary>
        /// Returns whether every character that can be matched lies in the ASCII range. A
        /// non-ASCII match disables certain initial-character optimizations in the tokenizer.
        /// </summary>
        /// <returns><see langword="true"/> when the set is ASCII-only.</returns>
        public override bool IsAscii()
        {
            if (Inverse)
            {
                return false;
            }
            for (int i = 0; i < _contents.Length; i++)
            {
                var obj = _contents[i];
                if (obj is char c)
                {
                    if (c is < (char)0 or >= (char)128)
                    {
                        return false;
                    }
                }
                else if (obj is Range)
                {
                    if (!((Range)obj).IsAscii())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Adds a single literal character to the set.
        /// </summary>
        /// <param name="c">The character to add.</param>
        public void AddCharacter(char c)
        {
            if (IgnoreCase)
            {
                c = Char.ToLower(c);
            }
            AddContent(c);
        }

        /// <summary>
        /// Adds an inclusive character range to the set.
        /// </summary>
        /// <param name="min">The lowest character in the range.</param>
        /// <param name="max">The highest character in the range.</param>
        public void AddRange(char min, char max)
        {
            if (IgnoreCase)
            {
                min = Char.ToLower(min);
                max = Char.ToLower(max);
            }
            AddContent(new Range(min, max));
        }

        private void AddContent(Object obj)
        {
            Array.Resize(ref _contents, _contents.Length + 1);
            _contents[_contents.Length - 1] = obj;
        }

        /// <summary>
        /// Returns whether <paramref name="ch"/> matches the set, applying the inversion flag.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when the character matches.</returns>
        public override bool Match(char ch)
        {
            object obj;
            char c;
            Range r;

            if (IgnoreCase)
            {
                ch = Char.ToLower(ch);
            }
            for (int i = 0; i < _contents.Length; i++)
            {
                obj = _contents[i];
                if (obj is char)
                {
                    c = (char)obj;
                    if (c == ch)
                    {
                        return !Inverse;
                    }
                }
                else if (obj is Range)
                {
                    r = (Range)obj;
                    if (r.Inside(ch))
                    {
                        return !Inverse;
                    }
                }
            }
            return Inverse;
        }

        /// <summary>
        /// Creates a new char-range transition pointing at <paramref name="state"/> and sharing
        /// the same content array.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            NFACharRangeTransition copy = new(Inverse, IgnoreCase, state) { _contents = _contents };
            return copy;
        }

        private class Range(char min, char max)
        {
            private readonly char _min = min;
            private readonly char _max = max;

            public bool IsAscii()
            {
                return 0 <= _min && _min < 128 &&
                       0 <= _max && _max < 128;
            }

            public bool Inside(char c)
            {
                return _min <= c && c <= _max;
            }
        }
    }
}
