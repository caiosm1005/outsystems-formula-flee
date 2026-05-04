using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    /// <summary>
    /// An <see cref="IVariable"/> backed by an <see cref="IDynamicExpression"/>. Reading the
    /// value evaluates the wrapped expression and casts the boxed result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The result type of the wrapped expression.</typeparam>
    internal class DynamicExpressionVariable<T> : IVariable, IGenericVariable<T>
    {
        private IDynamicExpression _myExpression = null!;

        /// <summary>
        /// Creates a copy that shares the same underlying expression.
        /// </summary>
        /// <returns>A new <see cref="DynamicExpressionVariable{T}"/>.</returns>
        public IVariable Clone()
        {
            DynamicExpressionVariable<T> copy = new()
            {
                _myExpression = _myExpression
            };
            return copy;
        }

        /// <summary>
        /// Evaluates the wrapped expression and returns its boxed result.
        /// </summary>
        /// <returns>The boxed evaluation result.</returns>
        public object GetValue()
        {
            return (T)_myExpression.Evaluate();
        }

        /// <summary>
        /// Gets or sets the wrapped <see cref="IDynamicExpression"/> as a boxed object.
        /// </summary>
        public object ValueAsObject
        {
            get => _myExpression;
            set => _myExpression = (value as IDynamicExpression)!;
        }

        /// <summary>
        /// Gets the result type declared by the wrapped expression's options.
        /// </summary>
        public Type VariableType => _myExpression.Context.Options.ResultType;
    }
}
