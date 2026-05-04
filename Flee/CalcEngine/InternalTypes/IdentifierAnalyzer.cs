using Flee.Parsing;
using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    /// <summary>
    /// Lightweight parser analyzer that walks an expression's parse tree and collects the
    /// names of leading-position identifiers in member expressions. Used by the calculation
    /// engine to discover which named expressions another expression depends on.
    /// </summary>
    internal class IdentifierAnalyzer : Analyzer
    {
        private readonly IDictionary<int, string> _myIdentifiers;
        private int _myMemberExpressionCount;

        private bool _myInFieldPropertyExpression;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public IdentifierAnalyzer()
        {
            _myIdentifiers = new Dictionary<int, string>();
        }

        /// <summary>
        /// Reacts to node-exit events from the parser, capturing identifiers and tracking
        /// field/property scope.
        /// </summary>
        /// <param name="node">The exiting node.</param>
        /// <returns>The same node (analyzers don't transform here).</returns>
        public override Node Exit(Node node)
        {
            switch (node.Id)
            {
                case (int)ExpressionConstants.IDENTIFIER:
                    ExitIdentifier((Token)node);
                    break;
                case (int)ExpressionConstants.FIELD_PROPERTY_EXPRESSION:
                    ExitFieldPropertyExpression();
                    break;
                default:
                    break;
            }

            return node;
        }

        /// <summary>
        /// Reacts to node-enter events from the parser, tracking member-expression depth and
        /// field/property scope.
        /// </summary>
        /// <param name="node">The entering node.</param>
        public override void Enter(Node node)
        {
            switch (node.Id)
            {
                case (int)ExpressionConstants.MEMBER_EXPRESSION:
                    EnterMemberExpression();
                    break;
                case (int)ExpressionConstants.FIELD_PROPERTY_EXPRESSION:
                    EnterFieldPropertyExpression();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Records the leading identifier of the current member expression, ignoring
        /// non-leading identifiers (those would be sub-member references).
        /// </summary>
        /// <param name="node">The identifier token.</param>
        private void ExitIdentifier(Token node)
        {
            if (!_myInFieldPropertyExpression)
            {
                return;
            }

            if (!_myIdentifiers.ContainsKey(_myMemberExpressionCount))
            {
                _myIdentifiers.Add(_myMemberExpressionCount, node.Image);
            }
        }

        /// <summary>
        /// Increments the member-expression counter so subsequent identifiers are bucketed
        /// per top-level expression.
        /// </summary>
        private void EnterMemberExpression()
        {
            _myMemberExpressionCount += 1;
        }

        /// <summary>
        /// Marks that we're inside a field/property reference so identifiers are captured.
        /// </summary>
        private void EnterFieldPropertyExpression()
        {
            _myInFieldPropertyExpression = true;
        }

        /// <summary>
        /// Marks that we've left a field/property reference.
        /// </summary>
        private void ExitFieldPropertyExpression()
        {
            _myInFieldPropertyExpression = false;
        }

        /// <summary>
        /// Resets the analyzer's state for reuse.
        /// </summary>
        public override void Reset()
        {
            _myIdentifiers.Clear();
            _myMemberExpressionCount = -1;
        }

        /// <summary>
        /// Returns the unique identifier names collected during the parse, filtered to exclude
        /// names that are namespaces or variables in <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The expression context.</param>
        /// <returns>The candidate identifier names.</returns>
        public ICollection<string> GetIdentifiers(ExpressionContext context)
        {
            Dictionary<string, object?> dict = new(StringComparer.OrdinalIgnoreCase);
            ExpressionImports ei = context.Imports;

            foreach (string identifier in _myIdentifiers.Values)
            {
                // Skip names registered as namespaces
                if (ei.HasNamespace(identifier))
                {
                    continue;
                }
                else if (context.Variables.ContainsKey(identifier))
                {
                    // Identifier is a variable
                    continue;
                }

                // Get only the unique values
                dict[identifier] = null;
            }

            return dict.Keys;
        }
    }
}
