namespace Flee.PublicTypes
{
    /// <summary>
    /// Common contract for compiled expressions, exposing metadata and the originating context.
    /// </summary>
    /// <remarks>
    /// Implementations are produced by <see cref="ExpressionContext.CompileDynamic"/>
    /// and <see cref="ExpressionContext.CompileGeneric{T}"/>.
    /// </remarks>
    public interface IExpression
    {
        /// <summary>
        /// Creates a deep copy of the expression so it can be evaluated independently.
        /// </summary>
        /// <returns>A new <see cref="IExpression"/> with its own context.</returns>
        IExpression Clone();

        /// <summary>
        /// Gets the original source text of the expression.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets metadata collected during compilation (e.g. referenced variable names).
        /// </summary>
        ExpressionInfo Info { get; }

        /// <summary>
        /// Gets the <see cref="ExpressionContext"/> the expression was compiled against.
        /// </summary>
        ExpressionContext Context { get; }

        /// <summary>
        /// Gets or sets the owner instance whose members are exposed to the expression.
        /// </summary>
        object? Owner { get; set; }
    }
}
