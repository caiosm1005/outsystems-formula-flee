using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class DynamicExpressionVariable<T> : IVariable, IGenericVariable<T>
    {
        private IDynamicExpression _myExpression = null!;
        public IVariable Clone()
        {
            DynamicExpressionVariable<T> copy = new DynamicExpressionVariable<T>();
            copy._myExpression = _myExpression;
            return copy;
        }

        public object GetValue()
        {
            return (T)_myExpression.Evaluate()!;
        }

        public object ValueAsObject
        {
            get { return _myExpression; }
            set { _myExpression = (value as IDynamicExpression)!; }
        }

        public System.Type VariableType => _myExpression.Context.Options.ResultType;
    }
}
