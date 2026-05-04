namespace Flee.PublicTypes
{
    /// <summary>
    /// Compiled expression whose result is returned as <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// Returned by <see cref="ExpressionContext.CompileDynamic"/>. Use
    /// <see cref="IGenericExpression{T}"/> when the result type is known at compile time.
    /// </remarks>
    public interface IDynamicExpression : IExpression
    {
        /// <summary>
        /// Evaluates the expression and returns the result boxed as <see cref="object"/>.
        /// </summary>
        /// <returns>The evaluation result.</returns>
        object Evaluate();
    }
}
