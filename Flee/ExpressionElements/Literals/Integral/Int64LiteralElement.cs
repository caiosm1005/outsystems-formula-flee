using System.Globalization;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    /// <summary>
    /// A 64-bit signed integer literal. Tracks a special "is <see cref="long.MinValue"/>" flag
    /// so the parser can construct the otherwise-unrepresentable value as
    /// <c>-9223372036854775808</c>.
    /// </summary>
    internal class Int64LiteralElement : IntegralLiteralElement
    {
        private Int64 _myValue;
        private const string MinValue = "9223372036854775808";
        private readonly bool _myIsMinValue;

        /// <summary>
        /// Initializes a new instance with the given value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        public Int64LiteralElement(Int64 value)
        {
            _myValue = value;
        }

        /// <summary>
        /// Initializes the special instance whose <see cref="Negate"/> produces
        /// <see cref="long.MinValue"/>.
        /// </summary>
        private Int64LiteralElement()
        {
            _myIsMinValue = true;
        }

        /// <summary>
        /// Attempts to create an <see cref="Int64LiteralElement"/> from a literal source string.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="isHex">Whether the literal was parsed as hexadecimal.</param>
        /// <param name="negated">Whether the literal was preceded by a unary minus.</param>
        /// <returns>The parsed element, or <see langword="null"/> when out of range.</returns>
        public static Int64LiteralElement? TryCreate(string image, bool isHex, bool negated)
        {
            if (negated & image == MinValue)
            {
                return new Int64LiteralElement();
            }
            else if (isHex)
            {
                return !Int64.TryParse(image, NumberStyles.AllowHexSpecifier, null, out long value)
                    ? null
                    : value >= 0 & value <= Int64.MaxValue ? new Int64LiteralElement(value) : null;
            }
            else
            {
                return Int64.TryParse(image, out long value) ? new Int64LiteralElement(value) : null;
            }
        }

        /// <summary>
        /// Emits the most compact load sequence for the constant value.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_myValue, ilg);
        }

        /// <summary>
        /// Applies a unary minus to the value, with special handling for
        /// <see cref="long.MinValue"/>.
        /// </summary>
        public void Negate()
        {
            _myValue = _myIsMinValue ? long.MinValue : -_myValue;
        }

        /// <summary>
        /// Gets <see cref="long"/>.
        /// </summary>
        public override Type ResultType => typeof(Int64);
    }
}
