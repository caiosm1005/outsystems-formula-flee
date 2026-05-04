using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// A <see cref="TimeSpan"/> literal. Parses the source via <see cref="TimeSpan.TryParse(string, out TimeSpan)"/>
    /// and emits a constructor call from ticks at evaluation time.
    /// </summary>
    internal class TimeSpanLiteralElement : LiteralElement
    {
        private readonly TimeSpan _myValue;

        /// <summary>
        /// Initializes a new instance by parsing <paramref name="image"/>, throwing a compile
        /// exception when the format isn't a valid <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        public TimeSpanLiteralElement(string image)
        {
            if (!TimeSpan.TryParse(image, out _myValue))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.CannotParseType,
                    CompileExceptionReason.InvalidFormat,
                    nameof(TimeSpan));
            }
        }

        /// <summary>
        /// Reconstructs the constant <see cref="TimeSpan"/> at runtime by calling the ticks
        /// constructor.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            int index = ilg.GetTempLocalIndex(typeof(TimeSpan));

            Utility.EmitLoadLocalAddress(ilg, index);

            EmitLoad(_myValue.Ticks, ilg);

            ConstructorInfo ci = typeof(TimeSpan).GetConstructor([typeof(long)])!;

            ilg.Emit(OpCodes.Call, ci);

            Utility.EmitLoadLocal(ilg, index);
        }

        /// <summary>
        /// Gets <see cref="TimeSpan"/>.
        /// </summary>
        public override Type ResultType => typeof(TimeSpan);
    }
}
