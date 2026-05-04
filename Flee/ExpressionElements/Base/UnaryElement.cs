using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    /// <summary>
    /// Base class for elements that operate on a single child operand (such as negate or
    /// logical-not).
    /// </summary>
    internal abstract class UnaryElement : ExpressionElement
    {
        /// <summary>
        /// The wrapped child element.
        /// </summary>
        protected ExpressionElement MyChild = null!;

        private Type? _myResultType;

        /// <summary>
        /// Sets the child and resolves the result type. Throws when the operation isn't defined
        /// for the child's type.
        /// </summary>
        /// <param name="child">The child element.</param>
        public void SetChild(ExpressionElement child)
        {
            MyChild = child;
            _myResultType = GetResultType(child.ResultType);

            if (_myResultType == null)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.OperationNotDefinedForType,
                    CompileExceptionReason.TypeMismatch,
                    MyChild.ResultType.Name);
            }
        }

        /// <summary>
        /// Computes the result type of this operation given the child's type, returning
        /// <see langword="null"/> when the operation isn't supported.
        /// </summary>
        /// <param name="childType">The child operand type.</param>
        /// <returns>The result type, or <see langword="null"/>.</returns>
        protected abstract Type? GetResultType(Type childType);

        /// <summary>
        /// Gets the resolved result type, set when <see cref="SetChild"/> is called.
        /// </summary>
        public override Type ResultType => _myResultType!;
    }
}
