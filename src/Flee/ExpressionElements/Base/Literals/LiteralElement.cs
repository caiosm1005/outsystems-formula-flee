using System.Reflection.Emit;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;


namespace Flee.ExpressionElements.Base.Literals
{
    internal abstract class LiteralElement : ExpressionElement
    {
        protected void OnParseOverflow(string image)
        {
            throw new ExpressionCompileException(Name, CompileErrorResourceKeys.ValueNotRepresentableInType,
                CompileExceptionReason.ConstantOverflow, image, ResultType.Name);
        }

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

        protected static void EmitLoad(bool value, FleeILGenerator ilg)
        {
            if (value == true)
            {
                ilg.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
            }
        }

        private static void EmitSuperShort(Int32 value, FleeILGenerator ilg)
        {
            OpCode ldcOpcode = default;

            ldcOpcode = value switch
            {
                0 => OpCodes.Ldc_I4_0,
                1 => OpCodes.Ldc_I4_1,
                2 => OpCodes.Ldc_I4_2,
                3 => OpCodes.Ldc_I4_3,
                4 => OpCodes.Ldc_I4_4,
                5 => OpCodes.Ldc_I4_5,
                6 => OpCodes.Ldc_I4_6,
                7 => OpCodes.Ldc_I4_7,
                8 => OpCodes.Ldc_I4_8,
                -1 => OpCodes.Ldc_I4_M1,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Value must be between -1 and 8 inclusive."),
            };
            ilg.Emit(ldcOpcode);
        }
    }
}
