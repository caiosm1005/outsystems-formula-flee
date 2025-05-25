using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    internal class UInt32LiteralElement : IntegralLiteralElement
    {
        private readonly UInt32 _myValue;
        public UInt32LiteralElement(UInt32 value)
        {
            _myValue = value;
        }

        public static bool TryCreate(string image, System.Globalization.NumberStyles ns, out UInt32LiteralElement result)
        {
            result = null;

            if (UInt32.TryParse(image, ns, null, out uint value))
            {
                result = new UInt32LiteralElement(value);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Convert.ToInt32(_myValue), ilg);
        }

        public override Type ResultType => typeof(UInt32);
    }
}
