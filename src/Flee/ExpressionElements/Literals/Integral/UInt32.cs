using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    internal class UInt32LiteralElement : IntegralLiteralElement
    {
        private readonly uint _myValue;
        public UInt32LiteralElement(uint value)
        {
            _myValue = value;
        }

        public static UInt32LiteralElement TryCreate(string image, System.Globalization.NumberStyles ns)
        {
            if (uint.TryParse(image, ns, null, out uint value))
            {
                return new UInt32LiteralElement(value);
            }
            else
            {
                return null;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Convert.ToInt32(_myValue), ilg);
        }

        public override Type ResultType => typeof(uint);
    }
}
