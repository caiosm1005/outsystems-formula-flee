namespace Flee.Parsing
{
    /**
     * A character range match transition. Used for user-defined
     * character sets in regular expressions.
     */
    internal class NFACharRangeTransition(bool inverse,
                                  bool ignoreCase,
                                  NFAState state) : NFATransition(state)
    {

        protected bool Inverse = inverse;
        protected bool IgnoreCase = ignoreCase;

        private object[] _contents = [];

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

        public void AddCharacter(char c)
        {
            if (IgnoreCase)
            {
                c = Char.ToLower(c);
            }
            AddContent(c);
        }

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
