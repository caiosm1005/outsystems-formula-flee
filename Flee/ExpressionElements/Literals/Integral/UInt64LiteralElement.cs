using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    /// <summary>
    /// A 64-bit unsigned integer literal.
    /// </summary>
    internal class UInt64LiteralElement : IntegralLiteralElement
    {
        private readonly UInt64 _myValue;

        /// <summary>
        /// Initializes a new instance from a literal source string. Reports a compile-time
        /// overflow when the value doesn't fit in <see cref="ulong"/>.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="ns">The number-style flags to use during parsing.</param>
        public UInt64LiteralElement(string image, System.Globalization.NumberStyles ns)
        {
            try
            {
                _myValue = UInt64.Parse(image, ns);
            }
            catch (OverflowException)
            {
                OnParseOverflow(image);
            }
        }

        /// <summary>
        /// Initializes a new instance with the given value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        public UInt64LiteralElement(UInt64 value)
        {
            _myValue = value;
        }

        /// <summary>
        /// Emits the value as a 64-bit constant (the IL representation is identical to
        /// <see cref="long"/>).
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Convert.ToInt64(_myValue), ilg);
        }

        /// <summary>
        /// Gets <see cref="ulong"/>.
        /// </summary>
        public override Type ResultType => typeof(UInt64);
    }
}
