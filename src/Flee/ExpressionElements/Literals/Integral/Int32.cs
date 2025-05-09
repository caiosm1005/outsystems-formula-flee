using System.Globalization;
using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals.Integral
{
    internal class Int32LiteralElement : IntegralLiteralElement
    {
        private int _myValue;
        private const string MinValue = "2147483648";
        private readonly bool _myIsMinValue;
        public Int32LiteralElement(int value)
        {
            _myValue = value;
        }

        private Int32LiteralElement()
        {
            _myIsMinValue = true;
        }

        public static Int32LiteralElement TryCreate(string image, bool isHex, bool negated)
        {
            if (negated == true & image == MinValue)
            {
                return new Int32LiteralElement();
            }
            else if (isHex == true)
            {
                // Since int.TryParse will succeed for a string like 0xFFFFFFFF we have to do some special handling
                if (int.TryParse(image, NumberStyles.AllowHexSpecifier, null, out int value) == false)
                {
                    return null;
                }
                else if (value >= 0 & value <= int.MaxValue)
                {
                    return new Int32LiteralElement(value);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (int.TryParse(image, out int value) == true)
                {
                    return new Int32LiteralElement(value);
                }
                else
                {
                    return null;
                }
            }
        }

        public void Negate()
        {
            if (_myIsMinValue == true)
            {
                _myValue = int.MinValue;
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

        public override Type ResultType => typeof(int);

        public int Value => _myValue;
    }
}
