using Flee.CalcEngine.InternalTypes;
using Flee.CalcEngine.PublicTypes;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>
    /// The compilation environment for an expression. Holds options, imports, variables, and
    /// the expression owner; produces compiled <see cref="IExpression"/> instances via
    /// <see cref="CompileDynamic"/> and <see cref="CompileGeneric{TResultType}"/>.
    /// </summary>
    public sealed class ExpressionContext
    {

        #region "Fields"

        private PropertyDictionary _myProperties;

#if NET9_0_OR_GREATER
        private readonly Lock _mySyncRoot = new();
#else
        private readonly object _mySyncRoot = new();
#endif
        #endregion

        #region "Constructor"

        /// <summary>
        /// Initializes a new context using the default expression owner.
        /// </summary>
        public ExpressionContext() : this(DefaultExpressionOwner.Instance)
        {
        }

        /// <summary>
        /// Initializes a new context with the given owner instance. The owner's members are
        /// exposed to expressions according to <see cref="ExpressionOptions.OwnerMemberAccess"/>.
        /// </summary>
        /// <param name="expressionOwner">The owner whose members are visible to expressions.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="expressionOwner"/> is <see langword="null"/>.
        /// </exception>
        public ExpressionContext(object expressionOwner)
        {
            Utility.AssertNotNull(expressionOwner, "expressionOwner");
            _myProperties = new PropertyDictionary();

            _myProperties.SetValue("CalculationEngine", null);
            _myProperties.SetValue("CalcEngineExpressionName", null);
            _myProperties.SetValue("IdentifierParser", null);

            _myProperties.SetValue("ExpressionOwner", expressionOwner);

            _myProperties.SetValue("ParserOptions", new ExpressionParserOptions(this));

            _myProperties.SetValue("Options", new ExpressionOptions(this));
            _myProperties.SetValue("Imports", new ExpressionImports());
            Imports.SetContext(this);
            Variables = new VariableCollection(this);

            _myProperties.SetToDefault<bool>("NoClone");

            RecreateParser();
        }

        #endregion

        #region "Methods - Private"

        /// <summary>
        /// Throws when a single (non-nested) type is not accessible to expressions in this context.
        /// </summary>
        /// <param name="t">The type to check.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the type is neither public nor co-located with the owner.
        /// </exception>
        private void AssertTypeIsAccessibleInternal(Type t)
        {
            bool isPublic = t.IsPublic;

            if (t.IsNested)
            {
                isPublic = t.IsNestedPublic;
            }

            bool isSameModuleAsOwner = ReferenceEquals(t.Module, ExpressionOwner.GetType().Module);

            // Public types are always accessible.  Otherwise they have to be in the same module as the owner
            bool isAccessible = isPublic | isSameModuleAsOwner;

            if (!isAccessible)
            {
                string msg = Utility.GetGeneralErrorMessage(
                    GeneralErrorResourceKeys.TypeNotAccessibleToExpression,
                    t.Name);
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// Walks a nested type chain and asserts every level is accessible.
        /// </summary>
        /// <param name="t">The leaf nested type.</param>
        private void AssertNestedTypeIsAccessible(Type t)
        {
            Type? current = t;
            while (current != null)
            {
                AssertTypeIsAccessibleInternal(current);
                current = current.DeclaringType;
            }
        }
        #endregion

        #region "Methods - Internal"

        /// <summary>
        /// Creates a copy of this context for use by a single compiled expression.
        /// </summary>
        /// <param name="cloneVariables">
        /// When <see langword="true"/>, also clones the variables; otherwise an empty collection is used.
        /// </param>
        /// <returns>The cloned context.</returns>
        internal ExpressionContext CloneInternal(bool cloneVariables)
        {
            ExpressionContext context = (ExpressionContext)MemberwiseClone();
            context._myProperties = _myProperties.Clone();
            context._myProperties.SetValue("Options", context.Options.Clone());
            context._myProperties.SetValue("ParserOptions", context.ParserOptions.Clone());
            context._myProperties.SetValue("Imports", context.Imports.Clone());
            context.Imports.SetContext(context);

            if (cloneVariables)
            {
                context.Variables = new VariableCollection(context);
                Variables.Copy(context.Variables);
            }

            return context;
        }

        /// <summary>
        /// Throws when <paramref name="t"/> (possibly nested) is not accessible to expressions.
        /// </summary>
        /// <param name="t">The type to check.</param>
        internal void AssertTypeIsAccessible(Type t)
        {
            if (t.IsNested)
            {
                AssertNestedTypeIsAccessible(t);
            }
            else
            {
                AssertTypeIsAccessibleInternal(t);
            }
        }

        /// <summary>
        /// Parses an expression string into an <see cref="ExpressionElement"/> tree, threading
        /// services through the analyzer.
        /// </summary>
        /// <param name="expression">The expression source text.</param>
        /// <param name="services">A service provider passed to the analyzer.</param>
        /// <returns>The root expression element.</returns>
        internal ExpressionElement Parse(string expression, IServiceProvider services)
        {
            lock (_mySyncRoot)
            {
                StringReader sr = new(expression);
                ExpressionParser parser = Parser;
                parser.Reset(sr);
                parser.Tokenizer.Reset(sr);
                FleeExpressionAnalyzer analyzer = (FleeExpressionAnalyzer)parser.Analyzer;

                analyzer.SetServices(services);

                Node rootNode = DoParse();
                analyzer.Reset();
                ExpressionElement topElement = (ExpressionElement)rootNode.Values[0]!;
                return topElement;
            }
        }

        /// <summary>
        /// Recreates the underlying parser instance — needed when parser-affecting options change.
        /// </summary>
        internal void RecreateParser()
        {
            lock (_mySyncRoot)
            {
                FleeExpressionAnalyzer analyzer = new();
                ExpressionParser parser = new(TextReader.Null, analyzer, this);
                _myProperties.SetValue("ExpressionParser", parser);
            }
        }

        /// <summary>
        /// Runs the parser and converts a <see cref="ParserLogException"/> into an
        /// <see cref="ExpressionCompileException"/>.
        /// </summary>
        /// <returns>The root parse tree node.</returns>
        /// <exception cref="ExpressionCompileException">
        /// Wraps any underlying <see cref="ParserLogException"/>.
        /// </exception>
        internal Node DoParse()
        {
            try
            {
                return Parser.Parse();
            }
            catch (ParserLogException ex)
            {
                // Syntax error; wrap it in our exception and rethrow
                throw new ExpressionCompileException(ex);
            }
        }

        /// <summary>
        /// Records the calculation engine and the name of this expression within it,
        /// so dependency tracking has the information it needs.
        /// </summary>
        /// <param name="engine">The owning <see cref="CalculationEngine"/>.</param>
        /// <param name="calcEngineExpressionName">The expression's registered name.</param>
        internal void SetCalcEngine(CalculationEngine engine, string calcEngineExpressionName)
        {
            _myProperties.SetValue("CalculationEngine", engine);
            _myProperties.SetValue("CalcEngineExpressionName", calcEngineExpressionName);
        }

        /// <summary>
        /// Parses an expression string with the lightweight <see cref="IdentifierAnalyzer"/>,
        /// which collects identifier references without producing IL.
        /// </summary>
        /// <param name="expression">The expression source text.</param>
        /// <returns>The analyzer holding the collected identifiers.</returns>
        internal IdentifierAnalyzer ParseIdentifiers(string expression)
        {
            ExpressionParser parser = IdentifierParser;
            StringReader sr = new(expression);
            parser.Reset(sr);
            parser.Tokenizer.Reset(sr);

            IdentifierAnalyzer analyzer = (IdentifierAnalyzer)parser.Analyzer;
            analyzer.Reset();

            _ = parser.Parse();

            return (IdentifierAnalyzer)parser.Analyzer;
        }
        #endregion

        #region "Methods - Public"

        /// <summary>
        /// Creates a deep copy of this context, including its variables.
        /// </summary>
        /// <returns>The cloned context.</returns>
        public ExpressionContext Clone()
        {
            return CloneInternal(true);
        }

        /// <summary>
        /// Compiles <paramref name="expression"/> into a dynamic expression whose result is
        /// returned as <see cref="object"/>.
        /// </summary>
        /// <param name="expression">The expression source text.</param>
        /// <returns>The compiled expression.</returns>
        /// <exception cref="ExpressionCompileException">Thrown when compilation fails.</exception>
        public IDynamicExpression CompileDynamic(string expression)
        {
            return new Expression<object>(expression, this, false);
        }

        /// <summary>
        /// Compiles <paramref name="expression"/> into a strongly typed expression that returns
        /// <typeparamref name="TResultType"/>.
        /// </summary>
        /// <typeparam name="TResultType">The result type the expression must evaluate to.</typeparam>
        /// <param name="expression">The expression source text.</param>
        /// <returns>The compiled expression.</returns>
        /// <exception cref="ExpressionCompileException">Thrown when compilation fails.</exception>
        public IGenericExpression<TResultType> CompileGeneric<TResultType>(string expression)
        {
            return new Expression<TResultType>(expression, this, true);
        }

        #endregion

        #region "Properties - Private"

        /// <summary>
        /// Gets a parser configured with the lightweight <see cref="IdentifierAnalyzer"/>,
        /// creating it lazily on first access.
        /// </summary>
        private ExpressionParser IdentifierParser
        {
            get
            {
                ExpressionParser parser = _myProperties.GetValue<ExpressionParser>("IdentifierParser");

                if (parser == null)
                {
                    IdentifierAnalyzer analyzer = new();
                    parser = new ExpressionParser(TextReader.Null, analyzer, this);
                    _myProperties.SetValue("IdentifierParser", parser);
                }

                return parser;
            }
        }

        #endregion

        #region "Properties - Internal"

        /// <summary>
        /// Gets or sets a value indicating whether the context should skip cloning when an
        /// expression is compiled (for advanced scenarios that share state across expressions).
        /// </summary>
        internal bool NoClone
        {
            get => _myProperties.GetValue<bool>("NoClone");
            set => _myProperties.SetValue("NoClone", value);
        }

        /// <summary>
        /// Gets the owner instance whose members are visible to expressions.
        /// </summary>
        internal object ExpressionOwner => _myProperties.GetValue<object>("ExpressionOwner");

        /// <summary>
        /// Gets the name this expression is registered under in its <see cref="CalculationEngine"/>,
        /// or an empty string when not part of one.
        /// </summary>
        internal string CalcEngineExpressionName =>
            _myProperties.GetValue<string>("CalcEngineExpressionName") ?? string.Empty;

        /// <summary>
        /// Gets the underlying <see cref="ExpressionParser"/>.
        /// </summary>
        internal ExpressionParser Parser => _myProperties.GetValue<ExpressionParser>("ExpressionParser");

        #endregion

        #region "Properties - Public"

        /// <summary>
        /// Gets the compile/evaluation options for this context.
        /// </summary>
        public ExpressionOptions Options => _myProperties.GetValue<ExpressionOptions>("Options");

        /// <summary>
        /// Gets the type/method imports available to expressions in this context.
        /// </summary>
        public ExpressionImports Imports => _myProperties.GetValue<ExpressionImports>("Imports");

        /// <summary>
        /// Gets the variable collection for this context.
        /// </summary>
        public VariableCollection Variables { get; private set; }

        /// <summary>
        /// Gets the <see cref="CalcEngine.PublicTypes.CalculationEngine"/> this context belongs to,
        /// or <see langword="null"/> when standalone.
        /// </summary>
        public CalculationEngine? CalculationEngine =>
            _myProperties.GetValue<CalculationEngine>("CalculationEngine");

        /// <summary>
        /// Gets the parser-level options for this context.
        /// </summary>
        public ExpressionParserOptions ParserOptions =>
            _myProperties.GetValue<ExpressionParserOptions>("ParserOptions");

        #endregion
    }
}
