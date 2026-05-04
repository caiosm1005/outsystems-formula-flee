using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A token node. Represents a token (i.e. a set of adjacent characters) in a parse tree.
    /// Tokens are produced by a <see cref="Tokenizer"/>, which groups characters together
    /// according to a set of token patterns.
    /// </summary>
    internal class Token : Node
    {
        private readonly int _startLine;
        private readonly int _startColumn;
        private readonly int _endLine;
        private readonly int _endColumn;
        private Token? _previous = null;
        private Token? _next = null;

        /// <summary>
        /// Initializes a new <see cref="Token"/> from the matched pattern, image, and
        /// starting source location.
        /// </summary>
        /// <param name="pattern">The pattern that produced this token.</param>
        /// <param name="image">The matched character sequence.</param>
        /// <param name="line">The starting line.</param>
        /// <param name="col">The starting column.</param>
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

        /// <summary>
        /// Gets the token pattern's id.
        /// </summary>
        public override int Id => Pattern.Id;

        /// <summary>
        /// Gets the token pattern's name.
        /// </summary>
        public override string Name => Pattern.Name;

        /// <summary>
        /// Gets the line where this token begins in the source.
        /// </summary>
        public override int StartLine => _startLine;

        /// <summary>
        /// Gets the column where this token begins in the source.
        /// </summary>
        public override int StartColumn => _startColumn;

        /// <summary>
        /// Gets the line where this token ends in the source.
        /// </summary>
        public override int EndLine => _endLine;

        /// <summary>
        /// Gets the column where this token ends in the source.
        /// </summary>
        public override int EndColumn => _endColumn;

        /// <summary>
        /// Gets the matched character sequence.
        /// </summary>
        public string Image { get; }

        /// <summary>
        /// Returns the matched character sequence.
        /// </summary>
        /// <returns>The image string.</returns>
        public string GetImage()
        {
            return Image;
        }

        internal TokenPattern Pattern { get; }

        /// <summary>
        /// Gets or sets the previous token in the doubly-linked token list. Setting this
        /// rewires the corresponding <see cref="Next"/> link of the new and previous siblings.
        /// </summary>
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

        /// <summary>
        /// Returns the previous token in the doubly-linked token list.
        /// </summary>
        /// <returns>The previous token, or <see langword="null"/> when none.</returns>
        public Token? GetPreviousToken()
        {
            return Previous;
        }

        /// <summary>
        /// Gets or sets the next token in the doubly-linked token list. Setting this rewires
        /// the corresponding <see cref="Previous"/> link of the new and previous siblings.
        /// </summary>
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

        /// <summary>
        /// Returns the next token in the doubly-linked token list.
        /// </summary>
        /// <returns>The next token, or <see langword="null"/> when none.</returns>
        public Token? GetNextToken()
        {
            return Next;
        }

        /// <summary>
        /// Returns a verbose textual description of this token, including its source position.
        /// </summary>
        /// <returns>The textual description.</returns>
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

        /// <summary>
        /// Returns a short textual description of this token suitable for inclusion in error
        /// messages.
        /// </summary>
        /// <returns>The short description.</returns>
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
