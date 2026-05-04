using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements.Literals.Real
{
    /// <summary>
    /// A <see cref="decimal"/> literal. Emits a call to the
    /// <c>decimal(int,int,int,bool,byte)</c> constructor with the bit pattern of the parsed value.
    /// </summary>
    internal class DecimalLiteralElement : RealLiteralElement
    {
        private static readonly ConstructorInfo OurConstructorInfo = GetConstructor()!;
        private readonly decimal _myValue;

        /// <summary>
        /// Initializes a placeholder used during overflow reporting.
        /// </summary>
        private DecimalLiteralElement()
        {
        }

        /// <summary>
        /// Initializes a new instance with the given value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        public DecimalLiteralElement(decimal value)
        {
            _myValue = value;
        }

        /// <summary>
        /// Returns the public 5-arg <see cref="decimal"/> constructor used during emit.
        /// </summary>
        /// <returns>The constructor metadata.</returns>
        private static ConstructorInfo? GetConstructor()
        {
            Type[] types = [
                typeof(Int32),
                typeof(Int32),
                typeof(Int32),
                typeof(bool),
                typeof(byte)
            ];
            return typeof(decimal).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public,
                null,
                CallingConventions.Any,
                types,
                null);
        }

        /// <summary>
        /// Parses <paramref name="image"/> as a <see cref="decimal"/> using the configured
        /// parse culture, reporting a compile-time overflow when out of range.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The parsed element, or <see langword="null"/> on overflow.</returns>
        public static DecimalLiteralElement? Parse(string image, IServiceProvider services)
        {
            ExpressionParserOptions options =
                (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions))!;
            DecimalLiteralElement element = new();

            try
            {
                decimal value = options.ParseDecimal(image);
                return new DecimalLiteralElement(value);
            }
            catch (OverflowException)
            {
                element.OnParseOverflow(image);
                return null;
            }
        }

        /// <summary>
        /// Reconstructs the constant <see cref="decimal"/> at runtime by calling its
        /// 5-arg constructor with the bit pattern produced by <see cref="decimal.GetBits"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            int index = ilg.GetTempLocalIndex(typeof(decimal));
            Utility.EmitLoadLocalAddress(ilg, index);

            int[] bits = decimal.GetBits(_myValue);
            EmitLoad(bits[0], ilg);
            EmitLoad(bits[1], ilg);
            EmitLoad(bits[2], ilg);

            int flags = bits[3];

            EmitLoad((flags >> 31) == -1, ilg);

            EmitLoad(flags >> 16, ilg);

            ilg.Emit(OpCodes.Call, OurConstructorInfo);

            Utility.EmitLoadLocal(ilg, index);
        }

        /// <summary>
        /// Gets <see cref="decimal"/>.
        /// </summary>
        public override Type ResultType => typeof(decimal);
    }
}
