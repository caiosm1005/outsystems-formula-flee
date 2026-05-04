using Flee.ExpressionElements.Base;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Wraps an expression element so that it is loaded from a local slot.
    /// </summary>
    internal class LocalBasedElement(ExpressionElement target, int index) : ExpressionElement
    {
        private readonly int _myIndex = index;

        private readonly ExpressionElement _myTarget = target;

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Utility.EmitLoadLocal(ilg, _myIndex);
        }

        public override Type ResultType => _myTarget.ResultType;
    }
}
