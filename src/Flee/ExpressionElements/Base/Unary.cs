using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    internal abstract class UnaryElement : ExpressionElement
    {

        protected ExpressionElement MyChild;

        private Type _myResultType;
        public void SetChild(ExpressionElement child)
        {
            MyChild = child;
            _myResultType = GetResultType(child.ResultType);

            if (_myResultType == null)
            {
                throw new ExpressionCompileException(Name, CompileErrorResourceKeys.OperationNotDefinedForType,
                    CompileExceptionReason.TypeMismatch, MyChild.ResultType.Name);
            }
        }

        protected abstract Type GetResultType(Type childType);

        public override Type ResultType => _myResultType;
    }

}
