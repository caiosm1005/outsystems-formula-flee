using System.Globalization;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    /// <summary>
    /// A 32-bit signed integer literal. Tracks a special "is <see cref="int.MinValue"/>" flag
    /// so the parser can construct the otherwise-unrepresentable value as <c>-2147483648</c>.
    /// </summary>
    internal class Int32LiteralElement : IntegralLiteralElement
    {
        private const string MinValue = "2147483648";
        private readonly bool _myIsMinValue;

        /// <summary>
        /// Initializes a new instance with the given value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        public Int32LiteralElement(Int32 value)
        {
            Value = value;
        }

        /// <summary>
        /// Initializes the special instance whose <see cref="Negate"/> produces
        /// <see cref="int.MinValue"/>. Used when the parser sees <c>-2147483648</c>.
        /// </summary>
        private Int32LiteralElement()
        {
            _myIsMinValue = true;
        }

        /// <summary>
        /// Attempts to create an <see cref="Int32LiteralElement"/> from a literal source string.
        /// Handles the negated <see cref="int.MinValue"/> edge case and rejects hex values that
        /// have the high bit set (those should fall through to <see cref="UInt32LiteralElement"/>).
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="isHex">Whether the literal was parsed as hexadecimal.</param>
        /// <param name="negated">Whether the literal was preceded by a unary minus.</param>
        /// <returns>The parsed element, or <see langword="null"/> when out of range.</returns>
        public static Int32LiteralElement? TryCreate(string image, bool isHex, bool negated)
        {
            if (negated & image == MinValue)
            {
                return new Int32LiteralElement();
            }
            else if (isHex)
            {
                // Since Int32.TryParse will succeed for a string like 0xFFFFFFFF we have to do some special handling
                return !Int32.TryParse(image, NumberStyles.AllowHexSpecifier, null, out int value)
                    ? null
                    : value >= 0 & value <= Int32.MaxValue ? new Int32LiteralElement(value) : null;
            }
            else
            {
                return Int32.TryParse(image, out int value) ? new Int32LiteralElement(value) : null;
            }
        }

        /// <summary>
        /// Applies a unary minus to <see cref="Value"/>, with special handling for
        /// <see cref="int.MinValue"/>.
        /// </summary>
        public void Negate()
        {
            Value = _myIsMinValue ? int.MinValue : -Value;
        }

        /// <summary>
        /// Emits the constant value with the most compact <c>ldc.i4</c> form.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Value, ilg);
        }

        /// <summary>
        /// Gets <see cref="int"/>.
        /// </summary>
        public override Type ResultType => typeof(Int32);

        /// <summary>
        /// Gets the parsed literal value.
        /// </summary>
        public int Value { get; private set; }
    }
}
