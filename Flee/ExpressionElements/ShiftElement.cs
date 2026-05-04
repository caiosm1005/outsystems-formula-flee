using System.Diagnostics;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Bit-shift operator (<c>&lt;&lt;</c> / <c>&gt;&gt;</c>). Forces the shift count into the
    /// 0..31 (32-bit operand) or 0..63 (64-bit operand) range to keep behavior defined.
    /// </summary>
    internal class ShiftElement : BinaryExpressionElement
    {
        private ShiftOperation _myOperation;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public ShiftElement()
        {
        }

        /// <summary>
        /// Validates that the left operand is integral and the right operand is convertible to
        /// <see cref="int"/>; returns the promoted result type.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <returns>The result type, or <see langword="null"/> when not supported.</returns>
        protected override Type? GetResultType(Type leftType, Type rightType)
        {
            // Right argument (shift count) must be convertible to int32
            if (!ImplicitConverter.EmitImplicitNumericConvert(rightType, typeof(Int32), null))
            {
                return null;
            }

            // Left argument must be an integer type
            if (!Utility.IsIntegralType(leftType))
            {
                return null;
            }

            TypeCode tc = Type.GetTypeCode(leftType);
            return tc is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                    ? typeof(Int32)
                    : tc == TypeCode.UInt32
                        ? typeof(UInt32)
                        : tc == TypeCode.Int64
                            ? typeof(Int64)
                            : tc == TypeCode.UInt64 ? typeof(UInt64) : null;
        }

        /// <summary>
        /// Stores the parsed <see cref="ShiftOperation"/>.
        /// </summary>
        /// <param name="operation">The operator value from the parser.</param>
        protected override void GetOperation(object operation)
        {
            _myOperation = (ShiftOperation)operation;
        }

        /// <summary>
        /// Emits the left operand, the masked shift count, and the appropriate shift opcode.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            MyLeftChild.Emit(ilg, services);
            EmitShiftCount(ilg, services);
            EmitShift(ilg);
        }

        /// <summary>
        /// Emits the right operand and ANDs it with the appropriate mask. If the shift count is
        /// greater than the number of bits in the number, the result is undefined — so we
        /// play it safe and force the shift count to 32/64 bits by ANDing it with the
        /// appropriate mask.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitShiftCount(FleeILGenerator ilg, IServiceProvider services)
        {
            MyRightChild.Emit(ilg, services);
            TypeCode tc = Type.GetTypeCode(MyLeftChild.ResultType);
            if (tc is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                or TypeCode.UInt32)
            {
                ilg.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(0x1f));
            }
            else if (tc is TypeCode.Int64 or TypeCode.UInt64)
            {
                ilg.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(0x3f));
            }
            else
            {
                Debug.Assert(false, "unknown left shift operand");
            }

            ilg.Emit(OpCodes.And);
        }

        /// <summary>
        /// Emits the appropriate shift opcode based on the left operand's signedness.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        private void EmitShift(FleeILGenerator ilg)
        {
            TypeCode tc = Type.GetTypeCode(MyLeftChild.ResultType);
            OpCode op;

            if (tc is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                or TypeCode.Int64)
            {
                // Signed operand, emit a left shift or arithmetic right shift
                op = _myOperation == ShiftOperation.LeftShift ? OpCodes.Shl : OpCodes.Shr;
            }
            else if (tc is TypeCode.UInt32 or TypeCode.UInt64)
            {
                // Unsigned operand, emit left shift or logical right shift
                op = _myOperation == ShiftOperation.LeftShift ? OpCodes.Shl : OpCodes.Shr_Un;
            }
            else
            {
                Debug.Assert(false, "unknown left shift operand");
                op = default;
            }

            ilg.Emit(op);
        }
    }
}
