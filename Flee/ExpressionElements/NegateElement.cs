using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Unary minus. Falls back to a user-defined <c>op_UnaryNegation</c> when present;
    /// otherwise emits the primitive <see cref="OpCodes.Neg"/> opcode.
    /// </summary>
    internal class NegateElement : UnaryElement
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public NegateElement()
        {
        }

        /// <summary>
        /// Resolves the result type. Prefers a user-defined <c>op_UnaryNegation</c>; otherwise
        /// returns the operand type for signed primitives, promotes <see cref="uint"/> to
        /// <see cref="long"/>, and returns <see langword="null"/> for everything else.
        /// </summary>
        /// <param name="childType">The operand type.</param>
        /// <returns>The result type, or <see langword="null"/> when not supported.</returns>
        protected override Type? GetResultType(Type childType)
        {
            MethodInfo? mi = Utility.GetSimpleOverloadedOperator("UnaryNegation", childType, null);
            if (mi != null)
            {
                return mi.ReturnType;
            }

            TypeCode tc = Type.GetTypeCode(childType);
            return tc is TypeCode.Single or TypeCode.Double or TypeCode.Int32 or TypeCode.Int64
                ? childType
                : tc == TypeCode.UInt32 ? typeof(Int64) : null;
        }

        /// <summary>
        /// Emits the child, applies any implicit conversion to the result type, then either
        /// calls the user-defined operator or the primitive <see cref="OpCodes.Neg"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type resultType = ResultType;
            MyChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyChild.ResultType, resultType, ilg);

            MethodInfo? mi = Utility.GetSimpleOverloadedOperator("UnaryNegation", resultType, null);

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
