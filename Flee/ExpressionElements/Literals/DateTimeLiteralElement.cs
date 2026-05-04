using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// A <see cref="DateTime"/> literal. Parses the source using
    /// <see cref="ExpressionParserOptions.DateTimeFormat"/> and emits a constructor call from
    /// ticks at evaluation time.
    /// </summary>
    internal class DateTimeLiteralElement : LiteralElement
    {
        private readonly DateTime _myValue;

        /// <summary>
        /// Initializes a new instance by parsing <paramref name="image"/> using the
        /// configured date/time format, throwing a compile exception when the format doesn't match.
        /// </summary>
        /// <param name="image">The literal source text.</param>
        /// <param name="context">The expression context (used for parser options).</param>
        public DateTimeLiteralElement(string image, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;

            if (!DateTime.TryParseExact(
                image,
                options.DateTimeFormat,
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
