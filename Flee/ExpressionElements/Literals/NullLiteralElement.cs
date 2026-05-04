using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// The literal <c>null</c>. Reports its result type as the <see cref="Null"/> sentinel so
    /// the type system can distinguish it from typed reference values.
    /// </summary>
    internal class NullLiteralElement : LiteralElement
    {
        /// <summary>
        /// Emits a <see cref="OpCodes.Ldnull"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldnull);
        }

        /// <summary>
        /// Gets the <see cref="Null"/> sentinel type.
        /// </summary>
        public override Type ResultType => typeof(Null);
    }
}
