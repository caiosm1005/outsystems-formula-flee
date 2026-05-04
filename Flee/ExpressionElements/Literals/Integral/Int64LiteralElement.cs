using System.Globalization;
using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    internal class Int64LiteralElement : IntegralLiteralElement
    {

        private Int64 _myValue;
        private const string MinValue = "9223372036854775808";

        private readonly bool _myIsMinValue;
        public Int64LiteralElement(Int64 value)
        {
            _myValue = value;
        }

        private Int64LiteralElement()
        {
            _myIsMinValue = true;
        }

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

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_myValue, ilg);
        }

        public void Negate()
        {
            _myValue = _myIsMinValue ? long.MinValue : -_myValue;
        }

        public override Type ResultType => typeof(Int64);
    }
}
