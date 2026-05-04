using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements.Literals.Real
{
    /// <summary>
    /// A <see cref="double"/> literal emitted via <see cref="OpCodes.Ldc_R8"/>.
    /// </summary>
    internal class DoubleLiteralElement : RealLiteralElement
    {
        private readonly double _myValue;

        /// <summary>
        /// Initializes a placeholder used during overflow reporting.
        /// </summary>
        private DoubleLiteralElement()
        {
        }

        /// <summary>
        /// Initializes a new instance with the given value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        public DoubleLiteralElement(double value)
        {
            _myValue = value;
        }

        /// <summary>
        /// Parses <paramref name="image"/> as a <see cref="double"/> using the configured
        /// parse culture, reporting a compile-time overflow when out of range.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The parsed element, or <see langword="null"/> on overflow.</returns>
        public static DoubleLiteralElement? Parse(string image, IServiceProvider services)
        {
            ExpressionParserOptions options =
                (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions))!;
            DoubleLiteralElement element = new();

            try
            {
                double value = options.ParseDouble(image);
                return new DoubleLiteralElement(value);
            }
            catch (OverflowException)
            {
                element.OnParseOverflow(image);
                return null;
            }
        }

        /// <summary>
        /// Emits the constant value with <see cref="OpCodes.Ldc_R8"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldc_R8, _myValue);
        }

        /// <summary>
        /// Gets <see cref="double"/>.
        /// </summary>
        public override Type ResultType => typeof(double);
    }
}
