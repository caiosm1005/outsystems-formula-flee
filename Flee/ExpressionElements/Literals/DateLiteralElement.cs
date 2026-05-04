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
    /// A date literal of the form <c>#yyyy-M-d#</c>. Stored as a <see cref="DateTime"/> with
    /// midnight time-of-day so it composes with the existing date arithmetic.
    /// </summary>
    internal class DateLiteralElement : LiteralElement
    {
        private const string Format = "yyyy-M-d";

        private readonly DateTime _myValue;

        /// <summary>
        /// Initializes a new instance by parsing <paramref name="image"/> using the fixed
        /// <c>yyyy-M-d</c> format, throwing a compile exception on a format mismatch.
        /// </summary>
        /// <param name="image">The literal source text without the surrounding <c>#</c> markers.</param>
        public DateLiteralElement(string image)
        {
            if (!DateTime.TryParseExact(
                image,
                Format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _myValue))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.CannotParseType,
                    CompileExceptionReason.InvalidFormat,
                    nameof(DateTime));
            }
        }

        /// <summary>
        /// Reconstructs the constant <see cref="DateTime"/> at runtime by calling the ticks
        /// constructor.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            int index = ilg.GetTempLocalIndex(typeof(DateTime));

            Utility.EmitLoadLocalAddress(ilg, index);

            EmitLoad(_myValue.Ticks, ilg);

            ConstructorInfo ci = typeof(DateTime).GetConstructor([typeof(long)])!;

            ilg.Emit(OpCodes.Call, ci);

            Utility.EmitLoadLocal(ilg, index);
        }

        /// <summary>
        /// Gets <see cref="DateTime"/>.
        /// </summary>
        public override Type ResultType => typeof(DateTime);
    }
}
