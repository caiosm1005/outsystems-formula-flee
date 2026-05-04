using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// A time-of-day literal of the form <c>#H:m:s#</c>. Stored as a <see cref="TimeSpan"/>
    /// so it composes with date/time arithmetic uniformly across every target framework
    /// (including those without <c>TimeOnly</c>).
    /// </summary>
    internal class TimeLiteralElement : LiteralElement
    {
        private const string Format = @"h\:m\:s";

        private readonly TimeSpan _myValue;

        /// <summary>
        /// Initializes a new instance by parsing <paramref name="image"/> using the fixed
        /// <c>H:m:s</c> format, throwing a compile exception on a format mismatch.
        /// </summary>
        /// <param name="image">The literal source text without the surrounding <c>#</c> markers.</param>
        public TimeLiteralElement(string image)
        {
            if (!TimeSpan.TryParseExact(
                image,
                Format,
                CultureInfo.InvariantCulture,
                TimeSpanStyles.None,
                out _myValue))
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
