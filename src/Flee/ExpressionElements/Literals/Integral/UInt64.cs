using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;


namespace Flee.ExpressionElements.Literals.Integral
{
    internal class UInt64LiteralElement : IntegralLiteralElement
    {
        private readonly ulong _myValue;
        
        public UInt64LiteralElement(string image, System.Globalization.NumberStyles ns)
        {
            try
            {
                _myValue = ulong.Parse(image, ns);
            }
            catch (OverflowException)
            {
                OnParseOverflow(image);
            }
        }

        public UInt64LiteralElement(ulong value)
        {
            _myValue = value;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(Convert.ToInt64(_myValue), ilg);
        }

        public override Type ResultType => typeof(ulong);
    }
}
