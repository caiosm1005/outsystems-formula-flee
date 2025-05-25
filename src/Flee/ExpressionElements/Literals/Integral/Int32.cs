using System.Globalization;
using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals.Integral
{
    internal class Int32LiteralElement : IntegralLiteralElement
    {
        private Int32 _myValue;
        private const string MinValue = "2147483648";
        private readonly bool _myIsMinValue;
        public Int32LiteralElement(Int32 value)
        {
            _myValue = value;
        }

        private Int32LiteralElement()
        {
            _myIsMinValue = true;
        }

        public static bool TryCreate(string image, bool isHex, bool negated, out Int32LiteralElement result)
        {
            result = null;

            if (negated == true & image == MinValue)
            {
                result = new Int32LiteralElement();
                return true;
            }
            else if (isHex == true)
            {

                // Since Int32.TryParse will succeed for a string like 0xFFFFFFFF we have to do some special handling
                if (Int32.TryParse(image, NumberStyles.AllowHexSpecifier, null, out int value) == false)
                {
                    return false;
                }
                else if (value >= 0 & value <= Int32.MaxValue)
                {
                    result = new Int32LiteralElement(value);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (Int32.TryParse(image, out int value) == true)
                {
                    result = new Int32LiteralElement(value);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Negate()
        {
            if (_myIsMinValue == true)
            {
                _myValue = Int32.MinValue;
            }
            else
            {
                _myValue = -_myValue;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_myValue, ilg);
        }

        public override Type ResultType => typeof(Int32);

        public int Value => _myValue;
    }
}
