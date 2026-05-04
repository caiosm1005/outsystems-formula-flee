using Flee.ExpressionElements.Base.Literals;

using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals
{
    internal class BooleanLiteralElement(bool value) : LiteralElement
    {
        private readonly bool _myValue = value;

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_myValue, ilg);
        }

        public override Type ResultType => typeof(bool);
    }
}
