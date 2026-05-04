using Flee.ExpressionElements.Base;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Wraps an expression element so that, on emit, its value is loaded from a previously
    /// allocated local slot rather than recomputed from the wrapped element.
    /// </summary>
    /// <param name="target">The element whose result type is forwarded.</param>
    /// <param name="index">The index of the local slot holding the precomputed value.</param>
    internal class LocalBasedElement(ExpressionElement target, int index) : ExpressionElement
    {
        private readonly int _myIndex = index;

        private readonly ExpressionElement _myTarget = target;

        /// <summary>
        /// Emits a load of the precomputed value from the configured local slot.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compilation service provider (unused).</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Utility.EmitLoadLocal(ilg, _myIndex);
        }

        /// <summary>
        /// Gets the result type of the wrapped element.
        /// </summary>
        public override Type ResultType => _myTarget.ResultType;
    }
}
