using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    /// <summary>
    /// A 32-bit unsigned integer literal.
    /// </summary>
    /// <param name="value">The literal value.</param>
    internal class UInt32LiteralElement(UInt32 value) : IntegralLiteralElement
    {
        private readonly UInt32 _myValue = value;

        /// <summary>
        /// Attempts to create a <see cref="UInt32LiteralElement"/> from a literal source string.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="ns">The number-style flags to use during parsing.</param>
        /// <returns>The parsed element, or <see langword="null"/> when out of range.</returns>
        public static UInt32LiteralElement? TryCreate(string image, System.Globalization.NumberStyles ns)
        {
            return UInt32.TryParse(image, ns, null, out uint value)
                ? new UInt32LiteralElement(value)
                : null;
        }

        /// <summary>
        /// Emits the value as a 32-bit constant (the IL representation is identical to
        /// <see cref="int"/>).
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Convert.ToInt32(_myValue), ilg);
        }

        /// <summary>
        /// Gets <see cref="uint"/>.
        /// </summary>
        public override Type ResultType => typeof(UInt32);
    }
}
