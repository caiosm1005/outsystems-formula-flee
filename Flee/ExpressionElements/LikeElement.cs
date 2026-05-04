using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// SQL <c>LIKE</c> operator. Matches the left-hand string against the right-hand SQL
    /// pattern (wildcards <c>%</c> any-many, <c>_</c> any-one, <c>[abc]</c> char class,
    /// <c>[^abc]</c> negated char class). Always evaluates case-insensitively to mirror
    /// SQL Server / OutSystems default semantics.
    /// </summary>
    internal class LikeElement : ExpressionElement
    {
        private static readonly MethodInfo _isMatchMethod =
            typeof(LikeRuntime).GetMethod(nameof(LikeRuntime.IsMatch),
                BindingFlags.Public | BindingFlags.Static)!;

        private readonly ExpressionElement _input;
        private readonly ExpressionElement _pattern;

        /// <summary>
        /// Initializes a new instance and validates that both operands resolve to strings.
        /// </summary>
        /// <param name="input">The left-hand string to test.</param>
        /// <param name="pattern">The right-hand SQL pattern.</param>
        public LikeElement(ExpressionElement input, ExpressionElement pattern)
        {
            _input = input;
            _pattern = pattern;
            Validate();
        }

        private void Validate()
        {
            if (!ReferenceEquals(_input.ResultType, typeof(string))
                || !ReferenceEquals(_pattern.ResultType, typeof(string)))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.OperationNotDefinedForTypes,
                    CompileExceptionReason.TypeMismatch,
                    "LIKE",
                    _input.ResultType.Name,
                    _pattern.ResultType.Name);
            }
        }

        /// <summary>
        /// Emits a call to <see cref="LikeRuntime.IsMatch"/> with the input and pattern.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _input.Emit(ilg, services);
            _pattern.Emit(ilg, services);
            ilg.Emit(OpCodes.Call, _isMatchMethod);
        }

        /// <summary>
        /// Always <see cref="bool"/>: <c>LIKE</c> tests for a match.
        /// </summary>
        public override Type ResultType => typeof(bool);
    }
}
