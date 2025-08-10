using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class GenericExpressionVariable<T> : IVariable, IGenericVariable<T>
    {
        private IGenericExpression<T> _myExpression;
        public IVariable Clone()
        {
            GenericExpressionVariable<T> copy = new GenericExpressionVariable<T>();
            copy._myExpression = _myExpression;
            return copy;
        }

        public object GetValue()
        {
            return _myExpression.Evaluate();
        }

        public object ValueAsObject
        {
            get { return _myExpression; }
            set { _myExpression = (IGenericExpression<T>)value; }
        }

        public System.Type VariableType => _myExpression.Context.Options.ResultType;
    }
}