using System.Diagnostics;
using System.Reflection;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;
using DateTime = Flee.PublicTypes.DateTime;

namespace Flee.ExpressionElements.Literals
{
    internal class DateTimeLiteralElement : LiteralElement
    {
        private readonly DateTime _myValue;

        public DateTimeLiteralElement(string image)
        {
            if (!DateTime.TryParse(image, out _myValue))
            {
                ThrowCompileException(CompileErrorResourceKeys.CannotParseType, CompileExceptionReason.InvalidFormat, typeof(DateTime).Name);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ConstructorInfo ci = typeof(DateTime).GetConstructor(new Type[] { typeof(long) });
            Debug.Assert(ci != null, "Constructor for DateTime not found.");
            EmitLoad(_myValue.Ticks, ilg);
            EmitNewObj(ci, ilg);
        }

        public override Type ResultType => typeof(DateTime);
    }
}
