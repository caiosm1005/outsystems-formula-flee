using System.Diagnostics;
using System.Reflection;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Literals
{
    internal class TimeLiteralElement : LiteralElement
    {
        private readonly Time _myValue;

        public TimeLiteralElement(string image)
        {
            if (!Time.TryParse(image, out _myValue))
            {
                ThrowCompileException(CompileErrorResourceKeys.CannotParseType, CompileExceptionReason.InvalidFormat, typeof(Time).Name);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ConstructorInfo ci = typeof(Time).GetConstructor(new[] { typeof(int) });
            Debug.Assert(ci != null, "Constructor for Time not found.");
            EmitLoad(_myValue.TotalSeconds, ilg);
            EmitNewObj(ci, ilg);
        }

        public override Type ResultType => typeof(Time);
    }
}
