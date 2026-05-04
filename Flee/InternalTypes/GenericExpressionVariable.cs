using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class GenericExpressionVariable<T> : IVariable, IGenericVariable<T>
    {
        private IGenericExpression<T> _myExpression = null!;
        public IVariable Clone()
        {
            GenericExpressionVariable<T> copy = new()
            {
                _myExpression = _myExpression
            };
            return copy;
        }

        public object GetValue()
        {
            return _myExpression.Evaluate()!;
        }

        public object ValueAsObject
        {
            get => _myExpression; set => _myExpression = (IGenericExpression<T>)value;
        }

        public Type VariableType => _myExpression.Context.Options.ResultType;
    }
}
