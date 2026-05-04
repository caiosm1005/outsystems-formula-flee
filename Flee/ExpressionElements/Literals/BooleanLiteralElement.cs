using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// A boolean literal (<c>true</c>/<c>false</c>) emitted as <c>ldc.i4.0</c>/<c>ldc.i4.1</c>.
    /// </summary>
    /// <param name="value">The literal value.</param>
    internal class BooleanLiteralElement(bool value) : LiteralElement
    {
        private readonly bool _myValue = value;

        /// <summary>
        /// Emits the constant boolean value onto the evaluation stack.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitLoad(_myValue, ilg);
        }

        /// <summary>
        /// Gets <see cref="bool"/>.
        /// </summary>
        public override Type ResultType => typeof(bool);
    }
}
