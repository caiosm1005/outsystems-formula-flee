using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    /// <summary>
    /// An <see cref="IVariable"/> backed by a strongly typed <see cref="IGenericExpression{T}"/>.
    /// Reading the value evaluates the wrapped expression and returns its result without boxing.
    /// </summary>
    /// <typeparam name="T">The result type of the wrapped expression.</typeparam>
    internal class GenericExpressionVariable<T> : IVariable, IGenericVariable<T>
    {
        private IGenericExpression<T> _myExpression = null!;

        /// <summary>
        /// Creates a copy that shares the same underlying expression.
        /// </summary>
        /// <returns>A new <see cref="GenericExpressionVariable{T}"/>.</returns>
        public IVariable Clone()
        {
            GenericExpressionVariable<T> copy = new()
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
            return _myExpression.Evaluate()!;
        }

        /// <summary>
        /// Gets or sets the wrapped <see cref="IGenericExpression{T}"/> as a boxed object.
        /// </summary>
        public object ValueAsObject
        {
            get => _myExpression;
            set => _myExpression = (IGenericExpression<T>)value;
        }

        /// <summary>
        /// Gets the result type declared by the wrapped expression's options.
        /// </summary>
        public Type VariableType => _myExpression.Context.Options.ResultType;
    }
}
