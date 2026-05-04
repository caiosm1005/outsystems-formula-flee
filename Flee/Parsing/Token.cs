using System.Text;

namespace Flee.Parsing
{
    /**
     * A token node. This class represents a token (i.e. a set of adjacent
     * characters) in a parse tree. The tokens are created by a tokenizer,
     * that groups characters together into tokens according to a set of
     * token patterns.
     */
    internal class Token : Node
    {
        private readonly int _startLine;
        private readonly int _startColumn;
        private readonly int _endLine;
        private readonly int _endColumn;
        private Token? _previous = null;
        private Token? _next = null;

        public Token(TokenPattern pattern, string image, int line, int col)
        {
            Pattern = pattern;
            Image = image;
            _startLine = line;
            _startColumn = col;
            _endLine = line;
            _endColumn = col + image.Length - 1;
            for (int pos = 0; image.IndexOf('\n', pos) >= 0;)
            {
                pos = image.IndexOf('\n', pos) + 1;
                _endLine++;
                _endColumn = image.Length - pos;
            }
        }

        public override int Id => Pattern.Id;

        public override string Name => Pattern.Name;

        public override int StartLine => _startLine;

        public override int StartColumn => _startColumn;

        public override int EndLine => _endLine;

        public override int EndColumn => _endColumn;

        public string Image { get; }

        public string GetImage()
        {
            return Image;
        }

        internal TokenPattern Pattern { get; }
        public Token? Previous
        {
            get => _previous;
            set
            {
                _ = (_previous?._next = null);
                _previous = value;
                _ = (_previous?._next = this);
            }
        }

        public Token? GetPreviousToken()
        {
            return Previous;
        }

        public Token? Next
        {
            get => _next;
            set
            {
                _ = (_next?._previous = null);
                _next = value;
                _ = (_next?._previous = this);
            }
        }

        public Token? GetNextToken()
        {
            return Next;
        }

        public override string ToString()
        {
            StringBuilder buffer = new();
            int newline = Image.IndexOf('\n');

            _ = buffer.Append(Pattern.Name);
            _ = buffer.Append("(");
            _ = buffer.Append(Pattern.Id);
            _ = buffer.Append("): \"");
            if (newline >= 0)
            {
                if (newline > 0 && Image[newline - 1] == '\r')
                {
                    newline--;
                }
                _ = buffer.Append(Image.Substring(0, newline));
                _ = buffer.Append("(...)");
            }
            else
            {
                _ = buffer.Append(Image);
            }
            _ = buffer.Append("\", line: ");
            _ = buffer.Append(_startLine);
            _ = buffer.Append(", col: ");
            _ = buffer.Append(_startColumn);

            return buffer.ToString();
        }

        public string ToShortString()
        {
            StringBuilder buffer = new();
            int newline = Image.IndexOf('\n');

            _ = buffer.Append('"');
            if (newline >= 0)
            {
                if (newline > 0 && Image[newline - 1] == '\r')
                {
                    newline--;
                }
                _ = buffer.Append(Image.Substring(0, newline));
                _ = buffer.Append("(...)");
            }
            else
            {
                _ = buffer.Append(Image);
            }
            _ = buffer.Append('"');
            if (Pattern.Type == TokenPattern.PatternType.REGEXP)
            {
                _ = buffer.Append(" <");
                _ = buffer.Append(Pattern.Name);
                _ = buffer.Append(">");
            }

            return buffer.ToString();
        }
    }
}
