using System.Globalization;
using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals.Integral
{
    internal class Int32LiteralElement : IntegralLiteralElement
    {
        private const string MinValue = "2147483648";
        private readonly bool _myIsMinValue;
        public Int32LiteralElement(Int32 value)
        {
            Value = value;
        }

        private Int32LiteralElement()
        {
            _myIsMinValue = true;
        }

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

        public void Negate()
        {
            Value = _myIsMinValue ? int.MinValue : -Value;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Value, ilg);
        }

        public override Type ResultType => typeof(Int32);

        public int Value { get; private set; }
    }
}
