using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.LogicalBitwise
{
    /// <summary>
    /// The unary <c>not</c> operator. For boolean operands emits a logical-not (compare-to-zero);
    /// for integral operands emits the bitwise-not <see cref="OpCodes.Not"/> opcode.
    /// </summary>
    internal class NotElement : UnaryElement
    {
        /// <summary>
        /// Emits a logical-not on booleans or a bitwise-not on integral operands.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            if (ReferenceEquals(MyChild.ResultType, typeof(bool)))
            {
                EmitLogical(ilg, services);
            }
            else
            {
                MyChild.Emit(ilg, services);
                ilg.Emit(OpCodes.Not);
            }
        }

        /// <summary>
        /// Emits a logical-not by comparing the boolean to zero.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitLogical(FleeILGenerator ilg, IServiceProvider services)
        {
            MyChild.Emit(ilg, services);
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
        }

        /// <summary>
        /// Booleans yield <see cref="bool"/>; integrals yield themselves; other types are unsupported.
        /// </summary>
        /// <param name="childType">The operand type.</param>
        /// <returns>The result type, or <see langword="null"/> when not supported.</returns>
        protected override Type? GetResultType(Type childType)
        {
            return ReferenceEquals(childType, typeof(bool))
                ? typeof(bool)
                : Utility.IsIntegralType(childType) ? childType : null;
        }
    }
}
