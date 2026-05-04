using System.Reflection.Emit;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// A string literal emitted via <see cref="OpCodes.Ldstr"/>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    internal class StringLiteralElement(string value) : LiteralElement
    {
        private readonly string _myValue = value;

        /// <summary>
        /// Emits an <c>ldstr</c> with the literal value.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            ilg.Emit(OpCodes.Ldstr, _myValue);
        }

        /// <summary>
        /// Gets <see cref="string"/>.
        /// </summary>
        public override Type ResultType => typeof(string);
    }
}
