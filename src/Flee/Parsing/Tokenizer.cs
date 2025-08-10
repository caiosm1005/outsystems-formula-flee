using System.Text;

namespace Flee.Parsing
{
    /**
      * A character stream tokenizer. This class groups the characters read
      * from the stream together into tokens ("words"). The grouping is
      * controlled by token patterns that contain either a fixed string to
      * search for, or a regular expression. If the stream of characters
      * don't match any of the token patterns, a parse exception is thrown.
      */
    internal class Tokenizer
    {
        private bool _useTokenList = false;
        private readonly StringDFAMatcher _stringDfaMatcher;
        private readonly NFAMatcher _nfaMatcher;
        private readonly RegExpMatcher _regExpMatcher;
        private ReaderBuffer _buffer = null;
        private readonly TokenMatch _lastMatch = new TokenMatch();
        private Token _previousToken = null;

        public Tokenizer(TextReader input)
            : this(input, false)
        {
        }

        public Tokenizer(TextReader input, bool ignoreCase)
        {
            this._stringDfaMatcher = new StringDFAMatcher(ignoreCase);
            this._nfaMatcher = new NFAMatcher(ignoreCase);
            this._regExpMatcher = new RegExpMatcher(ignoreCase);
            this._buffer = new ReaderBuffer(input);
        }

        public bool UseTokenList
        {
            get
            {
                return _useTokenList;
            }
            set
            {
                _useTokenList = value;
            }
        }

        public bool GetUseTokenList()
        {
            return _useTokenList;
        }

        public void SetUseTokenList(bool useTokenList)
        {
            this._useTokenList = useTokenList;
        }

        public string GetPatternDescription(int id)
        {
            var pattern = _stringDfaMatcher.GetPattern(id);
            if (pattern == null)
            {
                pattern = _nfaMatcher.GetPattern(id);
            }
            if (pattern == null)
            {
                pattern = _regExpMatcher.GetPattern(id);
            }
            return pattern?.ToShortString();
        }

        public int GetCurrentLine()
        {
            return _buffer.LineNumber;
        }

        public int GetCurrentColumn()
        {
            return _buffer.ColumnNumber;
        }

        /**
         * nfa - true to attempt as an nfa pattern for regexp. This handles most things except the complex repeates, ie {1,4}
         */
        public void AddPattern(TokenPattern pattern, bool nfa=true)
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

        public void Reset(TextReader input)
        {
            //this.buffer.Dispose();
            this._buffer = new ReaderBuffer(input);
            this._previousToken = null;
            this._lastMatch.Clear();
        }

        public Token Next()
        {
            Token token = null;

            do
            {
                token = NextToken();
                if (token == null)
                {
                    _previousToken = null;
                    return null;
                }
                if (_useTokenList)
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

        private Token NextToken()
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
                    var str = _buffer.Read(_lastMatch.Length);
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

        protected virtual Token NewToken(TokenPattern pattern,
                                         string image,
                                         int line,
                                         int column)
        {

            return new Token(pattern, image, line, column);
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append(_stringDfaMatcher);
            buffer.Append(_nfaMatcher);
            buffer.Append(_regExpMatcher);
            return buffer.ToString();
        }
    }
}
