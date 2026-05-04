using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Delegate signature of the dynamic method produced by an <see cref="Expression{T}"/>.
    /// The body is emitted as IL into a <see cref="System.Reflection.Emit.DynamicMethod"/>.
    /// </summary>
    /// <typeparam name="T">The expression's result type.</typeparam>
    /// <param name="owner">The expression owner instance, providing instance member access.</param>
    /// <param name="context">The compilation/evaluation context.</param>
    /// <param name="variables">The variables available to the expression.</param>
    /// <returns>The expression result.</returns>
    internal delegate T ExpressionEvaluator<T>(
        object owner,
        ExpressionContext context,
        VariableCollection variables);
}
