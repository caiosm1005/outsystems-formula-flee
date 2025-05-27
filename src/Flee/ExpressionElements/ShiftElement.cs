using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Specifies the types of shift operations supported by the Flee expression engine. Used to represent left and
    /// right shift operators in parsed expressions.
    /// </summary>
    internal enum ShiftOperation
    {
        LeftShift,
        RightShift
    }

    /// <summary>
    /// Represents a binary shift operation (left or right shift) within the Flee expression engine. Handles emitting IL
    /// for shift operations, including masking the shift count to the appropriate bit width.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ShiftElement"/> class with the specified operands and operation.
    /// </remarks>
    /// <param name="leftChild">The left operand (value to shift).</param>
    /// <param name="rightChild">The right operand (shift count).</param>
    /// <param name="operation">The shift operation to perform (left or right).</param>
    internal class ShiftElement(ExpressionElement leftChild, ExpressionElement rightChild, ShiftOperation operation) :
        BinaryExpressionElement(leftChild, rightChild, operation)
    {
        /// <summary>
        /// Emits the IL code for the shift count, masking it to the correct number of bits for the operand type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private void EmitShiftCount(FleeILGenerator ilg, IServiceProvider services)
        {
            // If the shift count is greater than the number of bits in the number, the result is undefined.
            // So we play it safe and force the shift count to 32/64 bits by ANDing it with the appropriate mask.
            _rightChild.Emit(ilg, services);

            TypeCode tc = Type.GetTypeCode(_leftChild.ResultType);

            switch (tc)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    ilg.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(0x1f));
                    break;

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    ilg.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(0x3f));
                    break;

                default:
                    throw new NotImplementedException(
                        $"Left shift operand for type {_leftChild.ResultType} not implemented.");
            }

            ilg.Emit(OpCodes.And);
        }

        /// <summary>
        /// Emits the IL code for the shift operation (left or right, signed or unsigned).
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        private void EmitShift(FleeILGenerator ilg)
        {
            OpCode op;
            TypeCode tc = Type.GetTypeCode(_leftChild.ResultType);

            switch (tc)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    // Signed operand, emit a left shift or arithmetic right shift
                    if ((ShiftOperation)_operation == ShiftOperation.LeftShift)
                    {
                        op = OpCodes.Shl;
                    }
                    else
                    {
                        op = OpCodes.Shr;
                    }
                    break;

                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    // Unsigned operand, emit left shift or logical right shift
                    if ((ShiftOperation)_operation == ShiftOperation.LeftShift)
                    {
                        op = OpCodes.Shl;
                    }
                    else
                    {
                        op = OpCodes.Shr_Un;
                    }
                    break;

                default:
                    throw new NotImplementedException(
                        $"Left shift operand for type {_leftChild.ResultType} not implemented.");
            }

            ilg.Emit(op);
        }

        /// <summary>
        /// Resolves the result type for the shift operation, ensuring the left operand is an integer type and the right
        /// operand is convertible to int.
        /// </summary>
        /// <param name="leftType">The type of the left operand.</param>
        /// <param name="rightType">The type of the right operand.</param>
        /// <returns>The result type, or null if the operation is not defined for the given types.</returns>
        protected override Type? ResolveResultType(Type leftType, Type rightType)
        {
            // Right argument (shift count) must be convertible to int32
            if (!ImplicitConverter.EmitImplicitNumericConvert(rightType, typeof(int), null))
            {
                return null;
            }

            // Left argument must be an integer type
            if (!Utility.IsIntegralType(leftType))
            {
                return null;
            }

            return Type.GetTypeCode(leftType) switch
            {
                TypeCode.Byte or
                TypeCode.SByte or
                TypeCode.Int16 or
                TypeCode.UInt16 or
                TypeCode.Int32 => typeof(int),
                TypeCode.UInt32 => typeof(uint),
                TypeCode.Int64 => typeof(long),
                TypeCode.UInt64 => typeof(ulong),
                _ => throw new NotImplementedException($"Left shift operand for type {leftType.Name} not implemented.")
            };
        }

        /// <summary>
        /// Emits the IL code for this shift expression element, including the left operand, masked shift count, and the
        /// shift operation.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _leftChild.Emit(ilg, services);
            EmitShiftCount(ilg, services);
            EmitShift(ilg);
        }
    }
}
