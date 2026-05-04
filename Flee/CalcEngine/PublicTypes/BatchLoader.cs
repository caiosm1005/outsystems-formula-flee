using Flee.CalcEngine.InternalTypes;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// Collects pending expression definitions and resolves them in topological order so that
    /// each expression sees its dependencies already compiled. Used to bulk-load a graph of
    /// expressions into a <see cref="CalculationEngine"/>.
    /// </summary>
    public sealed class BatchLoader
    {
        private readonly IDictionary<string, BatchLoadInfo> _myNameInfoMap;
        private readonly DependencyManager<string> _myDependencies;

        /// <summary>
        /// Initializes a new empty loader with case-insensitive name comparison.
        /// </summary>
        internal BatchLoader()
        {
            _myNameInfoMap = new Dictionary<string, BatchLoadInfo>(StringComparer.OrdinalIgnoreCase);
            _myDependencies = new DependencyManager<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Records an expression to be loaded. Parses the source text to discover dependencies
        /// so the eventual compile order is correct.
        /// </summary>
        /// <param name="atomName">The name to register the expression under.</param>
        /// <param name="expression">The expression source text.</param>
        /// <param name="context">The compilation context to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
        public void Add(string atomName, string expression, ExpressionContext context)
        {
            Utility.AssertNotNull(atomName, "atomName");
            Utility.AssertNotNull(expression, "expression");
            Utility.AssertNotNull(context, "context");

            BatchLoadInfo info = new(atomName, expression, context);
            _myNameInfoMap.Add(atomName, info);
            _myDependencies.AddTail(atomName);

            ICollection<string> references = GetReferences(expression, context);

            foreach (string reference in references)
            {
                _myDependencies.AddTail(reference);
                _myDependencies.AddDepedency(reference, atomName);
            }
        }

        /// <summary>
        /// Returns whether an expression with the given name has been added.
        /// </summary>
        /// <param name="atomName">The expression name.</param>
        /// <returns><see langword="true"/> when present.</returns>
        public bool Contains(string atomName)
        {
            return _myNameInfoMap.ContainsKey(atomName);
        }

        /// <summary>
        /// Returns the queued <see cref="BatchLoadInfo"/> entries in topological order.
        /// </summary>
        /// <returns>The sorted batch infos.</returns>
        internal BatchLoadInfo[] GetBachInfos()
        {
            string[] tails = _myDependencies.GetTails();
            Queue<string> sources = _myDependencies.GetSources(tails);

            IList<string> result = _myDependencies.TopologicalSort(sources);

            BatchLoadInfo[] infos = new BatchLoadInfo[result.Count];

            for (int i = 0; i <= result.Count - 1; i++)
            {
                infos[i] = _myNameInfoMap[result[i]];
            }

            return infos;
        }

        /// <summary>
        /// Parses <paramref name="expression"/> to collect the identifier names it references,
        /// using <see cref="IdentifierAnalyzer"/>.
        /// </summary>
        /// <param name="expression">The expression source text.</param>
        /// <param name="context">The compilation context.</param>
        /// <returns>The referenced identifier names.</returns>
        private ICollection<string> GetReferences(string expression, ExpressionContext context)
        {
            IdentifierAnalyzer analyzer = context.ParseIdentifiers(expression);

            return analyzer.GetIdentifiers(context);
        }
    }
}
