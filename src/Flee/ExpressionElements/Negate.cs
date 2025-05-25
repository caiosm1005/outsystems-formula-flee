using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements
{
    internal class NegateElement : UnaryElement
    {
        public NegateElement()
        {
        }

        protected override Type GetResultType(Type childType)
        {
            TypeCode tc = Type.GetTypeCode(childType);

            MethodInfo mi = Utility.GetSimpleOverloadedOperator("UnaryNegation", childType, null);
            if (mi != null)
            {
                return mi.ReturnType;
            }

            return tc switch
            {
                TypeCode.Single or
                TypeCode.Double or
                TypeCode.Int32 or
                TypeCode.Int64 => childType,
                TypeCode.UInt32 => typeof(Int64),
                _ => null,
            };
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type resultType = ResultType;
            MyChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(MyChild.ResultType, resultType, ilg);

            MethodInfo mi = Utility.GetSimpleOverloadedOperator("UnaryNegation", resultType, null);

            if (mi == null)
            {
                ilg.Emit(OpCodes.Neg);
            }
            else
            {
                ilg.Emit(OpCodes.Call, mi);
            }
        }
    }
}
