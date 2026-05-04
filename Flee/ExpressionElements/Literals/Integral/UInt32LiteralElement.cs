using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals.Integral
{
    internal class UInt32LiteralElement(UInt32 value) : IntegralLiteralElement
    {
        private readonly UInt32 _myValue = value;

        public static UInt32LiteralElement? TryCreate(string image, System.Globalization.NumberStyles ns)
        {
            return UInt32.TryParse(image, ns, null, out uint value) ? new UInt32LiteralElement(value) : null;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Convert.ToInt32(_myValue), ilg);
        }

        public override Type ResultType => typeof(UInt32);
    }
}
