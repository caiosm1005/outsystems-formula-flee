using System.Diagnostics;
using System.Reflection.Emit;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base.Literals
{
    /// <summary>
    /// Base class for elements that emit constant literal values. Provides shared parse-overflow
    /// reporting and compact <c>ldc</c> emit helpers used by integer and boolean literals.
    /// </summary>
    internal abstract class LiteralElement : ExpressionElement
    {
        /// <summary>
        /// Throws a "value not representable in type" compile exception. Subclasses call this
        /// when their parse step overflows the target type.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        protected void OnParseOverflow(string image)
        {
            ThrowCompileException(
                CompileErrorResourceKeys.ValueNotRepresentableInType,
                CompileExceptionReason.ConstantOverflow,
                image,
                ResultType.Name);
        }

        /// <summary>
        /// Emits the most compact <c>ldc.i4</c> variant for <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The 32-bit constant.</param>
        /// <param name="ilg">The IL generator.</param>
        public static void EmitLoad(Int32 value, FleeILGenerator ilg)
        {
            if (value >= -1 & value <= 8)
            {
                EmitSuperShort(value, ilg);
            }
            else if (value >= sbyte.MinValue & value <= sbyte.MaxValue)
            {
                ilg.Emit(OpCodes.Ldc_I4_S, Convert.ToSByte(value));
            }
            else
            {
                ilg.Emit(OpCodes.Ldc_I4, value);
            }
        }

        /// <summary>
        /// Emits the most compact load sequence for a 64-bit constant, falling back to
        /// <c>ldc.i8</c> when the value doesn't fit in 32-bit forms.
        /// </summary>
        /// <param name="value">The 64-bit constant.</param>
        /// <param name="ilg">The IL generator.</param>
        protected static void EmitLoad(Int64 value, FleeILGenerator ilg)
        {
            if (value >= Int32.MinValue & value <= Int32.MaxValue)
            {
                EmitLoad(Convert.ToInt32(value), ilg);
                ilg.Emit(OpCodes.Conv_I8);
            }
            else if (value >= 0 & value <= UInt32.MaxValue)
            {
                ilg.Emit(OpCodes.Ldc_I4, unchecked((int)Convert.ToUInt32(value)));
                ilg.Emit(OpCodes.Conv_U8);
            }
            else
            {
                ilg.Emit(OpCodes.Ldc_I8, value);
            }
        }

        /// <summary>
        /// Emits a 0/1 boolean constant.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <param name="ilg">The IL generator.</param>
        protected static void EmitLoad(bool value, FleeILGenerator ilg)
        {
            if (value)
            {
                ilg.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
            }
        }

        /// <summary>
        /// Emits the super-short <c>ldc.i4.N</c> variants for values in the range -1..8.
        /// </summary>
        /// <param name="value">The 32-bit constant.</param>
        /// <param name="ilg">The IL generator.</param>
        private static void EmitSuperShort(Int32 value, FleeILGenerator ilg)
        {
            OpCode ldcOpcode = default;

            switch (value)
            {
                case 0:
                    ldcOpcode = OpCodes.Ldc_I4_0;
                    break;
                case 1:
                    ldcOpcode = OpCodes.Ldc_I4_1;
                    break;
                case 2:
                    ldcOpcode = OpCodes.Ldc_I4_2;
                    break;
                case 3:
                    ldcOpcode = OpCodes.Ldc_I4_3;
                    break;
                case 4:
                    ldcOpcode = OpCodes.Ldc_I4_4;
                    break;
                case 5:
                    ldcOpcode = OpCodes.Ldc_I4_5;
                    break;
                case 6:
                    ldcOpcode = OpCodes.Ldc_I4_6;
                    break;
                case 7:
                    ldcOpcode = OpCodes.Ldc_I4_7;
                    break;
                case 8:
                    ldcOpcode = OpCodes.Ldc_I4_8;
                    break;
                case -1:
                    ldcOpcode = OpCodes.Ldc_I4_M1;
                    break;
                default:
                    Debug.Assert(false, "value out of range");
                    break;
            }

            ilg.Emit(ldcOpcode);
        }
    }
}
