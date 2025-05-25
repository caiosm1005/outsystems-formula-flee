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

        public static bool TryCreate(string image, bool isHex, bool negated, out Int64LiteralElement result)
        {
            result = null;

            if (negated == true & image == MinValue)
            {
                result = new Int64LiteralElement();
                return true;
            }
            else if (isHex == true)
            {
                if (Int64.TryParse(image, NumberStyles.AllowHexSpecifier, null, out long value) == false)
                {
                    return false;
                }
                else if (value >= 0 & value <= Int64.MaxValue)
                {
                    result = new Int64LiteralElement(value);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (Int64.TryParse(image, out long value) == true)
                {
                    result = new Int64LiteralElement(value);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_myValue, ilg);
        }

        public void Negate()
        {
            if (_myIsMinValue == true)
            {
                _myValue = Int64.MinValue;
            }
            else
            {
                _myValue = -_myValue;
            }
        }

        public override Type ResultType => typeof(Int64);
    }
}
