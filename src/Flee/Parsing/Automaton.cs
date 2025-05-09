﻿namespace Flee.Parsing
{
    internal class Automaton
    {
        private object _value;
        private readonly AutomatonTree _tree = new();

        public Automaton()
        {
        }

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

        public object MatchFrom(LookAheadReader input, int pos, bool caseInsensitive)
        {

            object result = null;
            int c = input.Peek(pos);
            if (_tree != null && c >= 0)
            {
                Automaton state = _tree.Find(Convert.ToChar(c), caseInsensitive);
                if (state != null)
                {
                    result = state.MatchFrom(input, pos + 1, caseInsensitive);
                }
            }
            return result ?? _value;
        }
    }

    // * An automaton state transition tree. This class contains a 
    // * binary search tree for the automaton transitions from one state 
    // * to another. All transitions are linked to a single character. 
    internal class AutomatonTree
    {
        private char _value;
        private Automaton _state;
        private AutomatonTree _left;
        private AutomatonTree _right;

        public AutomatonTree()
        {
        }

        public Automaton Find(char c, bool lowerCase)
        {
            if (lowerCase)
            {
                c = char.ToLower(c);
            }
            if (_value == (char)0 || _value == c)
            {
                return _state;
            }
            else if (_value > c)
            {
                return _left.Find(c, false);
            }
            else
            {
                return _right.Find(c, false);
            }
        }

        public void Add(char c, bool lowerCase, Automaton state)
        {
            if (lowerCase)
            {
                c = char.ToLower(c);
            }
            if (_value == (char)0)
            {
                _value = c;
                _state = state;
                _left = new AutomatonTree();
                _right = new AutomatonTree();
            }
            else if (_value > c)
            {
                _left.Add(c, false, state);
            }
            else
            {
                _right.Add(c, false, state);
            }
        }
    }
}
