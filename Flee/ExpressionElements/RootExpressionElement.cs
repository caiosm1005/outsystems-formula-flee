using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Wraps the top of the parse tree. Emits the child, applies any implicit conversion to the
    /// declared result type, boxes for non-generic expressions, and emits the final
    /// <see cref="OpCodes.Ret"/>.
    /// </summary>
    internal class RootExpressionElement : ExpressionElement
    {
        private readonly ExpressionElement _myChild;
        private readonly Type _myResultType;

        /// <summary>
        /// Initializes a new root and validates that the child's result is convertible to
        /// <paramref name="resultType"/>.
        /// </summary>
        /// <param name="child">The wrapped child element.</param>
        /// <param name="resultType">The declared expression result type.</param>
        public RootExpressionElement(ExpressionElement child, Type resultType)
        {
            _myChild = child;
            _myResultType = resultType;
            Validate();
        }

        /// <summary>
        /// Emits the child, then any required implicit conversion, then a box-to-object for
        /// non-generic dispatch, finishing with <see cref="OpCodes.Ret"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _myChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(_myChild.ResultType, _myResultType, ilg);

            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions))!;

            if (!options.IsGeneric)
            {
                _ = ImplicitConverter.EmitImplicitConvert(_myResultType, typeof(object), ilg);
            }

            ilg.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Throws a compile exception when the child's result type can't reach the declared
        /// result type.
        /// </summary>
        private void Validate()
        {
            if (!ImplicitConverter.EmitImplicitConvert(_myChild.ResultType, _myResultType, null))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.CannotConvertTypeToExpressionResult,
                    CompileExceptionReason.TypeMismatch,
                    _myChild.ResultType.Name,
                    _myResultType.Name);
            }
        }

        /// <summary>
        /// Always reports <see cref="object"/> because the dynamic dispatch path returns boxed.
        /// </summary>
        public override Type ResultType => typeof(object);
    }
}
