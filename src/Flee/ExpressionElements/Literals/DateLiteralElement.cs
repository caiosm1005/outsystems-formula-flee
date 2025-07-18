using System.Diagnostics;
using System.Reflection;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Literals
{
    internal class DateLiteralElement : LiteralElement
    {
        private readonly Date _myValue;

        public DateLiteralElement(string image)
        {
            if (!Date.TryParse(image, out _myValue))
            {
                ThrowCompileException(CompileErrorResourceKeys.CannotParseType, CompileExceptionReason.InvalidFormat, typeof(Date).Name);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ConstructorInfo ci = typeof(Date).GetConstructor(new[] { typeof(long) });
            Debug.Assert(ci != null, "Constructor for Date not found.");
            EmitLoad(_myValue.Ticks, ilg);
            EmitNewObj(ci, ilg);
        }

        public override Type ResultType => typeof(Date);
    }
}
