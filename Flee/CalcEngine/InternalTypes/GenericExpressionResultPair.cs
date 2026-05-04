namespace Flee.CalcEngine.InternalTypes
{
    /// <summary>
    /// Strongly typed <see cref="ExpressionResultPair"/> that stores its evaluation result as
    /// <typeparamref name="T"/> without boxing on the storage path.
    /// </summary>
    /// <typeparam name="T">The expression's result type.</typeparam>
    internal class GenericExpressionResultPair<T> : ExpressionResultPair
    {
        /// <summary>
        /// The most recent evaluation result.
        /// </summary>
        public T MyResult = default!;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public GenericExpressionResultPair()
        {
        }

        /// <summary>
        /// Re-runs the expression and stores the new result.
        /// </summary>
        public override void Recalculate()
        {
            MyResult = (T)MyExpression.Evaluate();
        }

        /// <summary>
        /// Gets the most recent evaluation result.
        /// </summary>
        public T Result => MyResult;

        /// <summary>
        /// Gets <typeparamref name="T"/>.
        /// </summary>
        public override Type ResultType => typeof(T);

        /// <summary>
        /// Gets or sets the boxed result of the most recent evaluation.
        /// </summary>
        public override object ResultAsObject
        {
            get => MyResult!;
            set => MyResult = (T)value;
        }
    }
}
