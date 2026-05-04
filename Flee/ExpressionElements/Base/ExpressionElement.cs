using System.Diagnostics;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    /// <summary>
    /// Root of the typed AST. Every parse-tree node is converted to an
    /// <see cref="ExpressionElement"/> subclass that knows how to emit its IL and report its
    /// result type.
    /// </summary>
    internal abstract class ExpressionElement
    {
        /// <summary>
        /// Initializes a new element. Internal so subclassing is restricted to the assembly.
        /// </summary>
        internal ExpressionElement()
        {
        }

        /// <summary>
        /// Emits the IL that evaluates this element and leaves its result on the stack.
        /// </summary>
        /// <param name="ilg">The IL generator to emit into.</param>
        /// <param name="services">Per-compile services (options, context, etc.).</param>
        public abstract void Emit(FleeILGenerator ilg, IServiceProvider services);

        /// <summary>
        /// Gets the CLR type the element evaluates to.
        /// </summary>
        public abstract Type ResultType { get; }

        /// <summary>
        /// Returns the localized element name, useful for diagnostics.
        /// </summary>
        /// <returns>The element name.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Throws an <see cref="ExpressionCompileException"/> using the localized template
        /// for <paramref name="messageKey"/> formatted with <paramref name="arguments"/>, and
        /// prefixed with this element's name.
        /// </summary>
        /// <param name="messageKey">The compile-error resource key.</param>
        /// <param name="reason">The reason category.</param>
        /// <param name="arguments">Format arguments for the message template.</param>
        protected void ThrowCompileException(
            string messageKey,
            CompileExceptionReason reason,
            params object[] arguments)
        {
            string messageTemplate = FleeResourceManager.Instance.GetCompileErrorString(messageKey) ?? messageKey;
            string message = string.Format(messageTemplate, arguments);
            message = string.Concat(Name, ": ", message);
            throw new ExpressionCompileException(message, reason);
        }

        /// <summary>
        /// Convenience helper that throws an "ambiguous overloaded operator" compile exception
        /// for the given operand types and operation.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <param name="operation">The operator description (typically the enum value).</param>
        protected void ThrowAmbiguousCallException(Type leftType, Type rightType, object operation)
        {
            ThrowCompileException(
                CompileErrorResourceKeys.AmbiguousOverloadedOperator,
                CompileExceptionReason.AmbiguousMatch,
                leftType.Name,
                rightType.Name,
                operation);
        }

        /// <summary>
        /// Gets the localized element name, looked up by the runtime type name in the resource bundle.
        /// </summary>
        protected string Name
        {
            get
            {
                string key = GetType().Name;
                string? value = FleeResourceManager.Instance.GetElementNameString(key);
                Debug.Assert(value != null, $"Element name for '{key}' not in resource file");
                return value!;
            }
        }
    }
}
