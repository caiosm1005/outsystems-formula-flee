namespace Flee.Parsing
{
    /**
     * A regular expression string element. This element only matches
     * an exact string. Once created, the string element is immutable.
     */
    internal class StringElement(string str) : Element
    {
        private readonly string _value = str;
        public StringElement(char c)
            : this(c.ToString())
        {
        }

        public string GetString()
        {
            return _value;
        }

        public override object Clone()
        {
            return this;
        }

        public override int Match(Matcher m,
                                  ReaderBuffer buffer,
                                  int start,
                                  int skip)
        {
            if (skip != 0)
            {
                return -1;
            }
            for (int i = 0; i < _value.Length; i++)
            {
                var c = buffer.Peek(start + i);
                if (c < 0)
                {
                    m.SetReadEndOfString();
                    return -1;
                }
                if (m.IsCaseInsensitive())
                {
                    c = Char.ToLower((char)c);
                }
                if (c != _value[i])
                {
                    return -1;
                }
            }
            return _value.Length;
        }

        public override void PrintTo(TextWriter output, string indent)
        {
            output.WriteLine(indent + "'" + _value + "'");
        }
    }
}
