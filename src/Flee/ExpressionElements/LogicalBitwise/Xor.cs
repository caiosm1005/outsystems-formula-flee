using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.LogicalBitwise
{
    internal class XorElement : BinaryExpressionElement
    {
        protected override Type GetResultType(Type leftType, Type rightType)
        {
            Type bitwiseType = Utility.GetBitwiseOpType(leftType, rightType);

            if (bitwiseType != null)
            {
                return bitwiseType;
            }
            else if (AreBothChildrenOfType(typeof(bool)) == true)
            {
                return typeof(bool);
            }
            else
            {
                return null;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type resultType = ResultType;

            MyLeftChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, resultType, ilg);
            MyRightChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, resultType, ilg);
            ilg.Emit(OpCodes.Xor);
        }


        protected override void GetOperation(object operation)
        {
        }
    }
}
