using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.LogicalBitwise
{
    internal class XorElement : BinaryExpressionElement
    {
        protected override Type? GetResultType(Type leftType, Type rightType)
        {
            Type? bitwiseType = Utility.GetBitwiseOpType(leftType, rightType);
            return bitwiseType ?? (AreBothChildrenOfType(typeof(bool)) ? typeof(bool) : null);
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type resultType = ResultType;

            MyLeftChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, resultType, ilg);
            MyRightChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, resultType, ilg);
            ilg.Emit(OpCodes.Xor);
        }


        protected override void GetOperation(object operation)
        {
        }
    }
}
