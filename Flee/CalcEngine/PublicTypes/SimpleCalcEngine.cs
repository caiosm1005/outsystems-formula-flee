using Flee.CalcEngine.InternalTypes;
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// Lightweight calculation engine that holds named compiled expressions but doesn't track
    /// dependencies or propagate changes — callers are responsible for re-evaluating manually.
    /// Use <see cref="CalculationEngine"/> when you need dependency-driven recalculation.
    /// </summary>
    public class SimpleCalcEngine
    {
        #region "Fields"
        private readonly IDictionary<string, IExpression> _myExpressions;
        #endregion

        #region "Constructor"

        /// <summary>
        /// Initializes a new instance with an empty expression map and a fresh context.
        /// </summary>
        public SimpleCalcEngine()
        {
            _myExpressions = new Dictionary<string, IExpression>(StringComparer.OrdinalIgnoreCase);
            Context = new ExpressionContext();
        }

        #endregion

        #region "Methods - Private"

        private void AddCompiledExpression(string expressionName, IExpression expression)
        {
            if (_myExpressions.ContainsKey(expressionName))
            {
                throw new InvalidOperationException(
                    $"The calc engine already contains an expression named '{expressionName}'");
            }
            else
            {
                _myExpressions.Add(expressionName, expression);
            }
        }

        private ExpressionContext ParseAndLink(string expressionName, string expression)
        {
            IdentifierAnalyzer analyzer = Context.ParseIdentifiers(expression);

            ExpressionContext context2 = Context.CloneInternal(true);
            LinkExpression(expressionName, context2, analyzer);

            // Tell the expression not to clone the context since it's already been cloned
            context2.NoClone = true;

            // Clear our context's variables
            Context.Variables.Clear();

            return context2;
        }

        private void LinkExpression(string expressionName, ExpressionContext context, IdentifierAnalyzer analyzer)
        {
            foreach (string identifier in analyzer.GetIdentifiers(context))
            {
                LinkIdentifier(identifier, expressionName, context);
            }
        }

        private void LinkIdentifier(string identifier, string expressionName, ExpressionContext context)
        {
            if (!_myExpressions.TryGetValue(identifier, out IExpression? child))
            {
                string msg = $"Expression '{expressionName}' references unknown name '{identifier}'";
                throw new InvalidOperationException(msg);
            }

            context.Variables.Add(identifier, child);
        }

        #endregion

        #region "Methods - Public"

        public void AddDynamic(string expressionName, string expression)
        {
            ExpressionContext linkedContext = ParseAndLink(expressionName, expression);
            IExpression e = linkedContext.CompileDynamic(expression);
            AddCompiledExpression(expressionName, e);
        }

        public void AddGeneric<T>(string expressionName, string expression)
        {
            ExpressionContext linkedContext = ParseAndLink(expressionName, expression);
            IExpression e = linkedContext.CompileGeneric<T>(expression);
            AddCompiledExpression(expressionName, e);
        }

        public void Clear()
        {
            _myExpressions.Clear();
        }

        #endregion

        #region "Properties - Public"
        public IExpression? this[string name]
        {
            get
            {
                _ = _myExpressions.TryGetValue(name, out IExpression? e);
                return e;
            }
        }

        public ExpressionContext Context { get; set; }
        #endregion
    }

}
