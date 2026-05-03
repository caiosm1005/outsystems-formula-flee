using Flee.ExpressionElements.Base;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Wraps an expression element so that it is loaded from a local slot.
    /// </summary>
    internal class LocalBasedElement : ExpressionElement
    {
        private readonly int _myIndex;

        private readonly ExpressionElement _myTarget;
        public LocalBasedElement(ExpressionElement target, int index)
        {
            _myTarget = target;
            _myIndex = index;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Utility.EmitLoadLocal(ilg, _myIndex);
        }

        public override System.Type ResultType => _myTarget.ResultType;
    }
}
