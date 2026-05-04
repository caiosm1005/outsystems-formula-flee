using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A character-stream tokenizer. Groups characters read from the stream into tokens
    /// ("words") according to a set of token patterns; each pattern carries either a fixed
    /// string to search for or a regular expression. If no token pattern matches the next
    /// characters in the stream, a <see cref="ParseException"/> is thrown.
    /// </summary>
    /// <param name="input">The input source.</param>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    internal class Tokenizer(TextReader input, bool ignoreCase)
    {
        private readonly StringDFAMatcher _stringDfaMatcher = new(ignoreCase);
        private readonly NFAMatcher _nfaMatcher = new(ignoreCase);
        private readonly RegExpMatcher _regExpMatcher = new(ignoreCase);
        private ReaderBuffer _buffer = new(input);
        private readonly TokenMatch _lastMatch = new();
        private Token? _previousToken = null;

        /// <summary>
        /// Initializes a new case-sensitive tokenizer.
        /// </summary>
        /// <param name="input">The input source.</param>
        public Tokenizer(TextReader input)
            : this(input, false)
        {
        }

        /// <summary>
        /// Gets or sets whether tokens should be linked together via
        /// <see cref="Token.Previous"/>/<see cref="Token.Next"/> as they are produced.
        /// </summary>
        public bool UseTokenList { get; set; } = false;

        /// <summary>
        /// Returns whether the tokenizer maintains a doubly-linked list of produced tokens.
        /// </summary>
        /// <returns><see langword="true"/> when token linking is enabled.</returns>
        public bool GetUseTokenList()
        {
            return UseTokenList;
        }

        /// <summary>
        /// Sets whether the tokenizer should maintain a doubly-linked list of produced tokens.
        /// </summary>
        /// <param name="useTokenList">The new value.</param>
        public void SetUseTokenList(bool useTokenList)
        {
            UseTokenList = useTokenList;
        }

        /// <summary>
        /// Returns the short description of the registered pattern with id <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The pattern id.</param>
        /// <returns>The short description, or an empty string when no pattern matches.</returns>
        public string GetPatternDescription(int id)
        {
            var pattern = GetPattern(id);
            return pattern?.ToShortString() ?? string.Empty;
        }

        /// <summary>
        /// Looks up the registered pattern with id <paramref name="id"/> across all matchers.
        /// </summary>
        /// <param name="id">The pattern id.</param>
        /// <returns>The matching pattern, or <see langword="null"/> when none.</returns>
        protected TokenPattern? GetPattern(int id)
        {
            var pattern = _stringDfaMatcher.GetPattern(id);
            pattern ??= _nfaMatcher.GetPattern(id);
            pattern ??= _regExpMatcher.GetPattern(id);
            return pattern;
        }

        /// <summary>
        /// Gets the input buffer. Subclasses may use this to peek ahead or unread characters
        /// when overriding <see cref="NextToken"/>.
        /// </summary>
        protected ReaderBuffer Buffer => _buffer;

        /// <summary>
        /// Returns the current line number reported by the input buffer.
        /// </summary>
        /// <returns>The current line number.</returns>
        public int GetCurrentLine()
        {
            return _buffer.LineNumber;
        }

        /// <summary>
        /// Returns the current column number reported by the input buffer.
        /// </summary>
        /// <returns>The current column number.</returns>
        public int GetCurrentColumn()
        {
            return _buffer.ColumnNumber;
        }

        /// <summary>
        /// Adds <paramref name="pattern"/> to the tokenizer, dispatching to the appropriate
        /// matcher based on the pattern type.
        /// </summary>
        /// <param name="pattern">The token pattern to add.</param>
        /// <param name="nfa">
        /// When <see langword="true"/> (the default), regex patterns are first tried against
        /// the NFA matcher. The NFA handles most regex features but not complex repeats like
        /// <c>{1,4}</c>; on failure the pattern falls back to the .NET regex engine.
        /// </param>
        /// <exception cref="ParserCreationException">If <paramref name="pattern"/> is invalid.</exception>
        public void AddPattern(TokenPattern pattern, bool nfa = true)
        {
            switch (pattern.Type)
            {
                case TokenPattern.PatternType.STRING:
                    try
                    {
                        _stringDfaMatcher.AddPattern(pattern);
                    }
                    catch (Exception e)
                    {
                        throw new ParserCreationException(
                            ParserCreationException.ErrorType.INVALID_TOKEN,
                            pattern.Name,
                            "error adding string token: " +
                            e.Message);
                    }
                    break;
                case TokenPattern.PatternType.REGEXP:
                    if (nfa)
                    {
                        try
                        {
                            _nfaMatcher.AddPattern(pattern);
                        }
                        catch (Exception)
                        {
                            nfa = false;
                        }
                    }
                    if (!nfa)
                    {
                        try
                        {
                            _regExpMatcher.AddPattern(pattern);
                        }
                        catch (Exception e)
                        {
                            throw new ParserCreationException(
                                ParserCreationException.ErrorType.INVALID_TOKEN,
                                pattern.Name,
                                "regular expression contains error(s): " +
                                e.Message);
                        }
                    }

                    break;
                default:
                    throw new ParserCreationException(
                        ParserCreationException.ErrorType.INVALID_TOKEN,
                        pattern.Name,
                        "pattern type " + pattern.Type +
                        " is undefined");
            }
        }

        /// <summary>
        /// Resets the tokenizer to read from a new input source.
        /// </summary>
        /// <param name="input">The new input source.</param>
        public void Reset(TextReader input)
        {
            //this.buffer.Dispose();
            _buffer = new ReaderBuffer(input);
            _previousToken = null;
            _lastMatch.Clear();
        }

        /// <summary>
        /// Reads and returns the next non-ignored token, or <see langword="null"/> at end of
        /// stream.
        /// </summary>
        /// <returns>The next token, or <see langword="null"/>.</returns>
        /// <exception cref="ParseException">If the next token signals an error.</exception>
        public Token? Next()
        {
            Token? token;

            do
            {
                token = NextToken();
                if (token == null)
                {
                    _previousToken = null;
                    return null;
                }
                if (UseTokenList)
                {
                    token.Previous = _previousToken;
                    _previousToken = token;
                }
                if (token.Pattern.Ignore)
                {
                    token = null;
                }
                else if (token.Pattern.Error)
                {
                    throw new ParseException(
                        ParseException.ErrorType.INVALID_TOKEN,
                        token.Pattern.ErrorMessage,
                        token.StartLine,
                        token.StartColumn);
                }
            } while (token == null);
            return token;
        }

        /// <summary>
        /// Reads the next token from the input buffer. Subclasses may override to suppress
        /// or replace tokens based on context (e.g. context-sensitive lexing). Returns
        /// <see langword="null"/> at end of stream.
        /// </summary>
        /// <returns>The next token, or <see langword="null"/>.</returns>
        /// <exception cref="ParseException">If matching fails.</exception>
        protected virtual Token? NextToken()
        {
            try
            {
                _lastMatch.Clear();
                _stringDfaMatcher.Match(_buffer, _lastMatch);
                _nfaMatcher.Match(_buffer, _lastMatch);
                _regExpMatcher.Match(_buffer, _lastMatch);
                int line;
                int column;
                if (_lastMatch.Length > 0)
                {
                    line = _buffer.LineNumber;
                    column = _buffer.ColumnNumber;
                    var str = _buffer.Read(_lastMatch.Length)!;
                    return NewToken(_lastMatch.Pattern, str, line, column);
                }
                else if (_buffer.Peek(0) < 0)
                {
                    return null;
                }
                else
                {
                    line = _buffer.LineNumber;
                    column = _buffer.ColumnNumber;
                    throw new ParseException(
                        ParseException.ErrorType.UNEXPECTED_CHAR,
                        _buffer.Read(1),
                        line,
                        column);
                }
            }
            catch (IOException e)
            {
                throw new ParseException(ParseException.ErrorType.IO,
                                         e.Message,
                                         -1,
                                         -1);
            }
        }

        /// <summary>
        /// Constructs a new <see cref="Token"/> for the matched pattern. Subclasses can
        /// override to substitute a custom token type.
        /// </summary>
        /// <param name="pattern">The matched pattern.</param>
        /// <param name="image">The matched character sequence.</param>
        /// <param name="line">The starting line.</param>
        /// <param name="column">The starting column.</param>
        /// <returns>The new token.</returns>
        protected virtual Token NewToken(TokenPattern pattern,
                                         string image,
                                         int line,
                                         int column)
        {

            return new Token(pattern, image, line, column);
        }

        /// <summary>
        /// Returns a textual description of every registered token matcher.
        /// </summary>
        /// <returns>The textual description.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new();
            _ = buffer.Append(_stringDfaMatcher);
            _ = buffer.Append(_nfaMatcher);
            _ = buffer.Append(_regExpMatcher);
            return buffer.ToString();
        }
    }
}
