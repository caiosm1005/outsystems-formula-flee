using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements.Literals.Real
{
    /// <summary>
    /// A <see cref="float"/> literal emitted via <see cref="OpCodes.Ldc_R4"/>.
    /// </summary>
    internal class SingleLiteralElement : RealLiteralElement
    {
        private readonly float _myValue;

        /// <summary>
        /// Initializes a placeholder used during overflow reporting.
        /// </summary>
        private SingleLiteralElement()
        {
        }

        /// <summary>
        /// Initializes a new instance with the given value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        public SingleLiteralElement(float value)
        {
            _myValue = value;
        }

        /// <summary>
        /// Parses <paramref name="image"/> as a <see cref="float"/> using the configured
        /// parse culture, reporting a compile-time overflow when out of range.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The parsed element, or <see langword="null"/> on overflow.</returns>
        public static SingleLiteralElement? Parse(string image, IServiceProvider services)
        {
            ExpressionParserOptions options =
                (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions))!;
            SingleLiteralElement element = new();

            try
            {
                float value = options.ParseSingle(image);
                return new SingleLiteralElement(value);
            }
            catch (OverflowException)
            {
                element.OnParseOverflow(image);
                return null;
            }
        }

        /// <summary>
        /// Emits the constant value with <see cref="OpCodes.Ldc_R4"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldc_R4, _myValue);
        }

        /// <summary>
        /// Gets <see cref="float"/>.
        /// </summary>
        public override Type ResultType => typeof(float);
    }
}
