using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.Literals
{
    /// <summary>
    /// A character literal (e.g. <c>'a'</c>) emitted as the integer value of the code point.
    /// </summary>
    /// <param name="value">The literal value.</param>
    internal class CharLiteralElement(char value) : LiteralElement
    {
        private readonly char _myValue = value;

        /// <summary>
        /// Emits the integer code point of the character.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            int intValue = Convert.ToInt32(_myValue);
            EmitLoad(intValue, ilg);
        }

        /// <summary>
        /// Gets <see cref="char"/>.
        /// </summary>
        public override Type ResultType => typeof(char);
    }
}
