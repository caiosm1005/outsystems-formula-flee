using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// Common base class for parsers. Provides the standard parser interface as well as
    /// shared token-handling, look-ahead, and analyzer-bridging logic.
    /// </summary>
    internal abstract class Parser
    {
        private bool _initialized;
        private readonly ArrayList _patterns = [];
        private readonly Hashtable _patternIds = [];
        private readonly ArrayList _tokens = [];
        private ParserLogException _errorLog = new();
        private int _errorRecovery = -1;

        /// <summary>
        /// Initializes a new parser that builds its own analyzer.
        /// </summary>
        /// <param name="input">The input source.</param>
        internal Parser(TextReader input) : this(input, null)
        {
        }

        /// <summary>
        /// Initializes a new parser with the supplied analyzer (or builds the default one).
        /// </summary>
        /// <param name="input">The input source.</param>
        /// <param name="analyzer">The analyzer to use, or <see langword="null"/> for the default.</param>
        internal Parser(TextReader input, Analyzer? analyzer)
        {
            Tokenizer = NewTokenizer(input);
            Analyzer = analyzer ?? NewAnalyzer();
        }

        /// <summary>
        /// Initializes a new parser with the supplied tokenizer.
        /// </summary>
        /// <param name="tokenizer">The tokenizer to use.</param>
        internal Parser(Tokenizer tokenizer) : this(tokenizer, null)
        {
        }

        /// <summary>
        /// Initializes a new parser with the supplied tokenizer and analyzer.
        /// </summary>
        /// <param name="tokenizer">The tokenizer to use.</param>
        /// <param name="analyzer">The analyzer to use, or <see langword="null"/> for the default.</param>
        internal Parser(Tokenizer tokenizer, Analyzer? analyzer)
        {
            Tokenizer = tokenizer;
            Analyzer = analyzer ?? NewAnalyzer();
        }

        /// <summary>
        /// Creates the tokenizer used to read tokens from <paramref name="input"/>. Subclasses
        /// override this to substitute a custom tokenizer.
        /// </summary>
        /// <param name="input">The input source.</param>
        /// <returns>The new tokenizer.</returns>
        protected virtual Tokenizer NewTokenizer(TextReader input)
        {
            // TODO: This method should really be abstract, but it isn't in this
            //       version due to backwards compatibility requirements.
            return new Tokenizer(input);
        }

        /// <summary>
        /// Creates the analyzer used by the parser. Subclasses override this to substitute a
        /// custom analyzer.
        /// </summary>
        /// <returns>The new analyzer.</returns>
        protected virtual Analyzer NewAnalyzer()
        {
            // TODO: This method should really be abstract, but it isn't in this
            //       version due to backwards compatibility requirements.
            return new Analyzer();
        }

        /// <summary>
        /// Gets the tokenizer used by this parser.
        /// </summary>
        public Tokenizer Tokenizer { get; }

        /// <summary>
        /// Gets the analyzer used by this parser.
        /// </summary>
        public Analyzer Analyzer { get; private set; }

        /// <summary>
        /// Returns the tokenizer used by this parser.
        /// </summary>
        /// <returns>The tokenizer.</returns>
        public Tokenizer GetTokenizer()
        {
            return Tokenizer;
        }

        /// <summary>
        /// Returns the analyzer used by this parser.
        /// </summary>
        /// <returns>The analyzer.</returns>
        public Analyzer GetAnalyzer()
        {
            return Analyzer;
        }

        internal void SetInitialized(bool initialized)
        {
            _initialized = initialized;
        }

        /// <summary>
        /// Adds a production pattern to the parser.
        /// </summary>
        /// <param name="pattern">The production pattern.</param>
        /// <exception cref="ParserCreationException">If the pattern is invalid or already registered.</exception>
        public virtual void AddPattern(ProductionPattern pattern)
        {
            if (pattern.Count <= 0)
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.INVALID_PRODUCTION,
                    pattern.Name,
                    "no production alternatives are present (must have at " +
                    "least one)");
            }
            if (_patternIds.ContainsKey(pattern.Id))
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.INVALID_PRODUCTION,
                    pattern.Name,
                    "another pattern with the same id (" + pattern.Id +
                    ") has already been added");
            }
            _ = _patterns.Add(pattern);
            _patternIds.Add(pattern.Id, pattern);
            SetInitialized(false);
        }

        /// <summary>
        /// Validates the registered patterns and prepares the parser for use. Subclasses can
        /// extend this to compute look-ahead tables.
        /// </summary>
        /// <exception cref="ParserCreationException">If the parser configuration is invalid.</exception>
        public virtual void Prepare()
        {
            if (_patterns.Count <= 0)
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.INVALID_PARSER,
                    "no production patterns have been added");
            }
            for (int i = 0; i < _patterns.Count; i++)
            {
                CheckPattern((ProductionPattern?)_patterns[i]);
            }
            SetInitialized(true);
        }

        private void CheckPattern(ProductionPattern? pattern)
        {
            if (pattern == null)
            {
                return;
            }

            for (int i = 0; i < pattern.Count; i++)
            {
                CheckAlternative(pattern.Name, pattern[i]);
            }
        }

        private void CheckAlternative(string name,
                                      ProductionPatternAlternative alt)
        {

            for (int i = 0; i < alt.Count; i++)
            {
                CheckElement(name, alt[i]);
            }
        }


        private void CheckElement(string name,
                                  ProductionPatternElement elem)
        {

            if (elem.IsProduction() && GetPattern(elem.Id) == null)
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.INVALID_PRODUCTION,
                    name,
                    "an undefined production pattern id (" + elem.Id +
                    ") is referenced");
            }
        }

        /// <summary>
        /// Resets the parser to read from a new input source.
        /// </summary>
        /// <param name="input">The new input source.</param>
        public void Reset(TextReader input)
        {
            Tokenizer.Reset(input);
            Analyzer.Reset();
        }

        /// <summary>
        /// Resets the parser to read from a new input source using <paramref name="analyzer"/>.
        /// </summary>
        /// <param name="input">The new input source.</param>
        /// <param name="analyzer">The analyzer to use.</param>
        public void Reset(TextReader input, Analyzer analyzer)
        {
            Tokenizer.Reset(input);
            Analyzer = analyzer;
        }

        /// <summary>
        /// Parses the input and returns the resulting parse tree.
        /// </summary>
        /// <returns>The root node of the parse tree.</returns>
        /// <exception cref="ParserLogException">If one or more parse errors were encountered.</exception>
        public Node Parse()
        {
            Node root = null!;

            // Initialize parser
            if (!_initialized)
            {
                Prepare();
            }
            _tokens.Clear();
            _errorLog = new ParserLogException();
            _errorRecovery = -1;

            // Parse input
            try
            {
                root = ParseStart();
            }
            catch (ParseException e)
            {
                AddError(e, true);
            }

            // Check for errors
            return _errorLog.Count > 0 ? throw _errorLog : root;
        }

        /// <summary>
        /// Parses the start production. Subclasses implement this to drive the actual parsing
        /// algorithm.
        /// </summary>
        /// <returns>The root node of the parse tree.</returns>
        protected abstract Node ParseStart();

        /// <summary>
        /// Creates a new production node for <paramref name="pattern"/>, delegating to the
        /// analyzer.
        /// </summary>
        /// <param name="pattern">The production pattern.</param>
        /// <returns>The new production node.</returns>
        protected virtual Production NewProduction(ProductionPattern pattern)
        {
            return Analyzer.NewProduction(pattern);
        }

        internal void AddError(ParseException e, bool recovery)
        {
            if (_errorRecovery <= 0)
            {
                _errorLog.AddError(e);
            }
            if (recovery)
            {
                _errorRecovery = 3;
            }
        }

        internal ProductionPattern? GetPattern(int id)
        {
            return (ProductionPattern?)_patternIds[id];
        }

        internal ProductionPattern? GetStartPattern()
        {
            return _patterns.Count <= 0 ? null : (ProductionPattern?)_patterns[0];
        }

        internal ICollection GetPatterns()
        {
            return _patterns;
        }

        internal void EnterNode(Node node)
        {
            if (!node.IsHidden() && _errorRecovery < 0)
            {
                try
                {
                    Analyzer.Enter(node);
                }
                catch (ParseException e)
                {
                    AddError(e, false);
                }
            }
        }

        internal Node ExitNode(Node node)
        {
            if (!node.IsHidden() && _errorRecovery < 0)
            {
                try
                {
                    return Analyzer.Exit(node);
                }
                catch (ParseException e)
                {
                    AddError(e, false);
                }
            }
            return node;
        }

        internal void AddNode(Production node, Node? child)
        {
            if (_errorRecovery >= 0)
            {
                // Do nothing
            }
            else if (node.IsHidden())
            {
                node.AddChild(child!);
            }
            else if (child != null && child.IsHidden())
            {
                for (int i = 0; i < child.Count; i++)
                {
                    AddNode(node, child[i]);
                }
            }
            else
            {
                try
                {
                    Analyzer.Child(node, child!);
                }
                catch (ParseException e)
                {
                    AddError(e, false);
                }
            }
        }

        internal Token NextToken()
        {
            Token? token = PeekToken(0);

            if (token != null)
            {
                _tokens.RemoveAt(0);
                return token;
            }
            else
            {
                throw new ParseException(
                    ParseException.ErrorType.UNEXPECTED_EOF,
                    null,
                    Tokenizer.GetCurrentLine(),
                    Tokenizer.GetCurrentColumn());
            }
        }

        internal Token NextToken(int id)
        {
            Token token = NextToken();

            if (token.Id == id)
            {
                if (_errorRecovery > 0)
                {
                    _errorRecovery--;
                }
                return token;
            }
            else
            {
                ArrayList list = [Tokenizer.GetPatternDescription(id)];
                throw new ParseException(
                    ParseException.ErrorType.UNEXPECTED_TOKEN,
                    token.ToShortString(),
                    list,
                    token.StartLine,
                    token.StartColumn);
            }
        }

        internal Token? PeekToken(int steps)
        {
            while (steps >= _tokens.Count)
            {
                try
                {
                    var token = Tokenizer.Next();
                    if (token == null)
                    {
                        return null;
                    }
                    else
                    {
                        _ = _tokens.Add(token);
                    }
                }
                catch (ParseException e)
                {
                    AddError(e, true);
                }
            }
            return (Token?)_tokens[steps];
        }

        /// <summary>
        /// Returns a textual description of every production pattern registered with this
        /// parser.
        /// </summary>
        /// <returns>The textual description.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new();

            for (int i = 0; i < _patterns.Count; i++)
            {
                _ = buffer.Append(ToString((ProductionPattern)_patterns[i]!));
                _ = buffer.Append("\n");
            }
            return buffer.ToString();
        }

        private string ToString(ProductionPattern prod)
        {
            StringBuilder buffer = new();
            StringBuilder indent = new();
            int i;

            _ = buffer.Append(prod.Name);
            _ = buffer.Append(" (");
            _ = buffer.Append(prod.Id);
            _ = buffer.Append(") ");
            for (i = 0; i < buffer.Length; i++)
            {
                _ = indent.Append(" ");
            }
            _ = buffer.Append("= ");
            _ = indent.Append("| ");
            for (i = 0; i < prod.Count; i++)
            {
                if (i > 0)
                {
                    _ = buffer.Append(indent);
                }
                _ = buffer.Append(ToString(prod[i]));
                _ = buffer.Append("\n");
            }
            for (i = 0; i < prod.Count; i++)
            {
                var set = prod[i].LookAhead;
                if (set.GetMaxLength() > 1)
                {
                    _ = buffer.Append("Using ");
                    _ = buffer.Append(set.GetMaxLength());
                    _ = buffer.Append(" token look-ahead for alternative ");
                    _ = buffer.Append(i + 1);
                    _ = buffer.Append(": ");
                    _ = buffer.Append(set.ToString(Tokenizer));
                    _ = buffer.Append("\n");
                }
            }
            return buffer.ToString();
        }

        private string ToString(ProductionPatternAlternative alt)
        {
            StringBuilder buffer = new();

            for (int i = 0; i < alt.Count; i++)
            {
                if (i > 0)
                {
                    _ = buffer.Append(" ");
                }
                _ = buffer.Append(ToString(alt[i]));
            }
            return buffer.ToString();
        }

        private string ToString(ProductionPatternElement elem)
        {
            StringBuilder buffer = new();
            int min = elem.MinCount;
            int max = elem.MaxCount;

            if (min == 0 && max == 1)
            {
                _ = buffer.Append("[");
            }
            _ = buffer.Append(elem.IsToken() ? GetTokenDescription(elem.Id) : GetPattern(elem.Id)!.Name);
            if (min == 0 && max == 1)
            {
                _ = buffer.Append("]");
            }
            else if (min == 0 && max == Int32.MaxValue)
            {
                _ = buffer.Append("*");
            }
            else if (min == 1 && max == Int32.MaxValue)
            {
                _ = buffer.Append("+");
            }
            else if (min != 1 || max != 1)
            {
                _ = buffer.Append("{");
                _ = buffer.Append(min);
                _ = buffer.Append(",");
                _ = buffer.Append(max);
                _ = buffer.Append("}");
            }
            return buffer.ToString();
        }

        internal string GetTokenDescription(int token)
        {
            return Tokenizer == null ? "" : Tokenizer.GetPatternDescription(token);
        }
    }
}
