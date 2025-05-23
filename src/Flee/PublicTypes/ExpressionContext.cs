using Flee.CalcEngine.InternalTypes;
using Flee.CalcEngine.PublicTypes;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public sealed class ExpressionContext
    {

        #region "Fields"

        private PropertyDictionary _myProperties;

        private readonly object _mySyncRoot = new();

        private VariableCollection _myVariables;
        #endregion

        #region "Constructor"

        public ExpressionContext() : this(DefaultExpressionOwner.Instance)
        {
        }

        public ExpressionContext(object expressionOwner)
        {
            if (expressionOwner == null)
            {
                throw new ArgumentNullException(nameof(expressionOwner));
            }
            
            _myProperties = new PropertyDictionary();

            _myProperties.SetValue("CalculationEngine", null);
            _myProperties.SetValue("CalcEngineExpressionName", null);
            _myProperties.SetValue("IdentifierParser", null);

            _myProperties.SetValue("ExpressionOwner", expressionOwner);

            _myProperties.SetValue("ParserOptions", new ExpressionParserOptions(this));

            _myProperties.SetValue("Options", new ExpressionOptions(this));
            _myProperties.SetValue("Imports", new ExpressionImports());
            Imports.SetContext(this);
            _myVariables = new VariableCollection(this);

            _myProperties.SetToDefault<bool>("NoClone");

            RecreateParser();
        }

        #endregion

        #region "Methods - Private"

        private void AssertTypeIsAccessibleInternal(Type t)
        {
            bool isPublic = t.IsPublic;

            if (t.IsNested == true)
            {
                isPublic = t.IsNestedPublic;
            }

            bool isSameModuleAsOwner = ReferenceEquals(t.Module, ExpressionOwner.GetType().Module);

            // Public types are always accessible.  Otherwise they have to be in the same module as the owner
            bool isAccessible = isPublic | isSameModuleAsOwner;

            if (isAccessible == false)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.TypeNotAccessibleToExpression, t.Name);
                throw new ArgumentException(msg);
            }
        }

        private void AssertNestedTypeIsAccessible(Type t)
        {
            while (t != null)
            {
                AssertTypeIsAccessibleInternal(t);
                t = t.DeclaringType;
            }
        }
        #endregion

        #region "Methods - Internal"
        internal ExpressionContext CloneInternal(bool cloneVariables)
        {
            ExpressionContext context = (ExpressionContext)MemberwiseClone();
            context._myProperties = _myProperties.Clone();
            context._myProperties.SetValue("Options", context.Options.Clone());
            context._myProperties.SetValue("ParserOptions", context.ParserOptions.Clone());
            context._myProperties.SetValue("Imports", context.Imports.Clone());
            context.Imports.SetContext(context);

            if (cloneVariables == true)
            {
                context._myVariables = new VariableCollection(context);
                Variables.Copy(context._myVariables);
            }

            return context;
        }

        internal void AssertTypeIsAccessible(Type t)
        {
            if (t.IsNested == true)
            {
                AssertNestedTypeIsAccessible(t);
            }
            else
            {
                AssertTypeIsAccessibleInternal(t);
            }
        }

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
                ExpressionElement topElement = (ExpressionElement)rootNode.Values[0];
                return topElement;
            }
        }

        internal void RecreateParser()
        {
            lock (_mySyncRoot)
            {
                FleeExpressionAnalyzer analyzer = new();
                ExpressionParser parser = new(TextReader.Null, analyzer, this);
                _myProperties.SetValue("ExpressionParser", parser);
            }
        }

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

        internal void SetCalcEngine(CalculationEngine engine, string calcEngineExpressionName)
        {
            _myProperties.SetValue("CalculationEngine", engine);
            _myProperties.SetValue("CalcEngineExpressionName", calcEngineExpressionName);
        }

        internal IdentifierAnalyzer ParseIdentifiers(string expression)
        {
            ExpressionParser parser = IdentifierParser;
            StringReader sr = new(expression);
            parser.Reset(sr);
            parser.Tokenizer.Reset(sr);

            IdentifierAnalyzer analyzer = (IdentifierAnalyzer)parser.Analyzer;
            analyzer.Reset();

            parser.Parse();

            return (IdentifierAnalyzer)parser.Analyzer;
        }
        #endregion

        #region "Methods - Public"

        public ExpressionContext Clone()
        {
            return CloneInternal(true);
        }

        public IDynamicExpression CompileDynamic(string expression)
        {
            return new Expression<object>(expression, this, false);
        }

        public IGenericExpression<TResultType> CompileGeneric<TResultType>(string expression)
        {
            return new Expression<TResultType>(expression, this, true);
        }

        #endregion

        #region "Properties - Private"

        private ExpressionParser IdentifierParser
        {
            get
            {
                ExpressionParser parser = _myProperties.GetValue<ExpressionParser>("IdentifierParser");

                if (parser == null)
                {
                    IdentifierAnalyzer analyzer = new();
                    parser = new ExpressionParser(TextReader.Null, analyzer, this);
                    //parser = new ExpressionParser(System.IO.StringReader.Null, analyzer, this);
                    _myProperties.SetValue("IdentifierParser", parser);
                }

                return parser;
            }
        }

        #endregion

        #region "Properties - Internal"

        internal bool NoClone
        {
            get { return _myProperties.GetValue<bool>("NoClone"); }
            set { _myProperties.SetValue("NoClone", value); }
        }

        internal object ExpressionOwner => _myProperties.GetValue<object>("ExpressionOwner");

        internal string CalcEngineExpressionName => _myProperties.GetValue<string>("CalcEngineExpressionName");

        internal ExpressionParser Parser => _myProperties.GetValue<ExpressionParser>("ExpressionParser");

        #endregion

        #region "Properties - Public"
        public ExpressionOptions Options => _myProperties.GetValue<ExpressionOptions>("Options");

        public ExpressionImports Imports => _myProperties.GetValue<ExpressionImports>("Imports");

        public VariableCollection Variables => _myVariables;

        public CalculationEngine CalculationEngine => _myProperties.GetValue<CalculationEngine>("CalculationEngine");

        public ExpressionParserOptions ParserOptions => _myProperties.GetValue<ExpressionParserOptions>("ParserOptions");

        #endregion
    }
}
