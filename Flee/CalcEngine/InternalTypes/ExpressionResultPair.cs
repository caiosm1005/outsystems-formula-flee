using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    /// <summary>
    /// Pairs a named compiled expression with its most recent evaluation result. Subclasses
    /// specialize on the result type so the engine can store strongly typed values without boxing.
    /// </summary>
    internal abstract class ExpressionResultPair
    {
        /// <summary>
        /// The compiled expression evaluated by this pair.
        /// </summary>
        protected IDynamicExpression MyExpression = null!;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        protected ExpressionResultPair()
        {
        }

        /// <summary>
        /// Re-runs <see cref="MyExpression"/> and stores the new result.
        /// </summary>
        public abstract void Recalculate();

        /// <summary>
        /// Sets the expression backing this pair.
        /// </summary>
        /// <param name="e">The compiled expression.</param>
        public void SetExpression(IDynamicExpression e)
        {
            MyExpression = e;
        }

        /// <summary>
        /// Sets the engine-registered name of this expression.
        /// </summary>
        /// <param name="name">The expression name.</param>
        public void SetName(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the expression name.
        /// </summary>
        /// <returns>The expression name.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Gets the engine-registered name of this expression.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the CLR type of <see cref="ResultAsObject"/>.
        /// </summary>
        public abstract Type ResultType { get; }

        /// <summary>
        /// Gets or sets the boxed result of the most recent evaluation.
        /// </summary>
        public abstract object ResultAsObject { get; set; }

        /// <summary>
        /// Gets the compiled expression backing this pair.
        /// </summary>
        public IDynamicExpression Expression => MyExpression;
    }
}
