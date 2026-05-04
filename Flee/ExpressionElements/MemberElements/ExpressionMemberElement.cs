using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// A <see cref="MemberElement"/> wrapping an arbitrary <see cref="ExpressionElement"/> as
    /// the head of a member chain. Used for parenthesized expressions that are then dereferenced.
    /// </summary>
    /// <param name="element">The wrapped expression element.</param>
    internal class ExpressionMemberElement(ExpressionElement element) : MemberElement
    {
        private readonly ExpressionElement _myElement = element;

        /// <summary>
        /// Nothing to resolve — the wrapped element is already validated.
        /// </summary>
        protected override void ResolveInternal()
        {
        }

        /// <summary>
        /// Emits the wrapped element and, when the result is a value type, prepares its address
        /// for the next link in the dereference chain.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            base.Emit(ilg, services);
            _myElement.Emit(ilg, services);
            if (_myElement.ResultType.IsValueType)
            {
                EmitValueTypeLoadAddress(ilg, ResultType);
            }
        }

        /// <summary>
        /// Always supports instance use.
        /// </summary>
        protected override bool SupportsInstance => true;

        /// <summary>
        /// Public, since arbitrary expressions have no access modifiers themselves.
        /// </summary>
        protected override bool IsPublic => true;

        /// <summary>
        /// Always non-static — wrapped expressions act as instance values.
        /// </summary>
        public override bool IsStatic => false;

        /// <summary>
        /// Never an extension method.
        /// </summary>
        public override bool IsExtensionMethod => false;

        /// <summary>
        /// Gets the result type of the wrapped element.
        /// </summary>
        public override Type ResultType => _myElement.ResultType;
    }
}
