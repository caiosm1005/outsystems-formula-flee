namespace Flee.PublicTypes
{
    /// <summary>
    /// Compiled expression whose result type is statically known.
    /// </summary>
    /// <typeparam name="T">The CLR type the expression evaluates to.</typeparam>
    /// <remarks>
    /// Returned by <see cref="ExpressionContext.CompileGeneric{T}"/>. Avoids the boxing
    /// performed by <see cref="IDynamicExpression"/>.
    /// </remarks>
    public interface IGenericExpression<T> : IExpression
    {
        /// <summary>
        /// Evaluates the expression and returns its strongly typed result.
        /// </summary>
        /// <returns>The evaluation result.</returns>
        T Evaluate();
    }
}
