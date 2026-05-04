using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// Thrown by <see cref="BatchLoader"/> when one of the expressions in a batch fails to
    /// compile. Wraps the underlying <see cref="ExpressionCompileException"/> and exposes the
    /// problem expression by name and source.
    /// </summary>
    public class BatchLoadCompileException : Exception
    {
        /// <summary>
        /// Initializes a new instance describing a batch-compile failure.
        /// </summary>
        /// <param name="atomName">The expression's name in the batch.</param>
        /// <param name="expressionText">The expression source text.</param>
        /// <param name="innerException">The underlying compile failure.</param>
        internal BatchLoadCompileException(
            string atomName,
            string expressionText,
            ExpressionCompileException innerException)
            : base(
                $"Batch Load: The expression for atom '${atomName}' could not be compiled",
                innerException)
        {
            AtomName = atomName;
            ExpressionText = expressionText;
        }

        /// <summary>
        /// Gets the name of the expression that failed to compile.
        /// </summary>
        public string AtomName { get; }

        /// <summary>
        /// Gets the source text of the expression that failed to compile.
        /// </summary>
        public string ExpressionText { get; }
    }
}
