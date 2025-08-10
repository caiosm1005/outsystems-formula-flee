namespace Flee.Parsing
{
    internal class Automaton
    {
        private object _value;
        private readonly AutomatonTree _tree = new AutomatonTree();

        public Automaton()
        {
        }

        public void AddMatch(string str, bool caseInsensitive, object value)
        {
            if (str.Length == 0)
            {
                this._value = value;
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
            Automaton state = null;
            int c = 0;

            c = input.Peek(pos);
            if (_tree != null && c >= 0)
            {
                state = _tree.Find(Convert.ToChar(c), caseInsensitive);
                if (state != null)
                {
                    result = state.MatchFrom(input, pos + 1, caseInsensitive);
                }
            }
            return result ?? _value;
        }
    }
}
