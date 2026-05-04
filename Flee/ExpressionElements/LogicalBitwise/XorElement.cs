using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.LogicalBitwise
{
    /// <summary>
    /// The <c>xor</c> operator. Supports both bitwise (integral operands) and logical
    /// (boolean operands) variants — both compile to <see cref="OpCodes.Xor"/>.
    /// </summary>
    internal class XorElement : BinaryExpressionElement
    {
        /// <summary>
        /// Returns the bitwise result type for integrals, <see cref="bool"/> when both operands
        /// are boolean, otherwise <see langword="null"/>.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <returns>The result type, or <see langword="null"/>.</returns>
        protected override Type? GetResultType(Type leftType, Type rightType)
        {
            Type? bitwiseType = Utility.GetBitwiseOpType(leftType, rightType);
            return bitwiseType ?? (AreBothChildrenOfType(typeof(bool)) ? typeof(bool) : null);
        }

        /// <summary>
        /// Emits both children with conversion to the result type, then <see cref="OpCodes.Xor"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type resultType = ResultType;

            MyLeftChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, resultType, ilg);
            MyRightChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, resultType, ilg);
            ilg.Emit(OpCodes.Xor);
        }

        /// <summary>
        /// XOR has no per-operator state to capture.
        /// </summary>
        /// <param name="operation">Ignored.</param>
        protected override void GetOperation(object operation)
        {
        }
    }
}
