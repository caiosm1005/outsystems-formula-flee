using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// The conditional <c>if(cond, t, f)</c> expression. Validates that <c>cond</c> is boolean
    /// and that one of <c>t</c>/<c>f</c> is implicitly convertible to the other; the chosen
    /// type becomes the result type.
    /// </summary>
    internal class ConditionalElement : ExpressionElement
    {
        private readonly ExpressionElement _myCondition;
        private readonly ExpressionElement _myWhenTrue;
        private readonly ExpressionElement _myWhenFalse;
        private readonly Type _myResultType = null!;

        /// <summary>
        /// Initializes a new instance and resolves the result type from the branches.
        /// </summary>
        /// <param name="condition">The boolean condition.</param>
        /// <param name="whenTrue">The value when the condition is true.</param>
        /// <param name="whenFalse">The value when the condition is false.</param>
        public ConditionalElement(
            ExpressionElement condition,
            ExpressionElement whenTrue,
            ExpressionElement whenFalse)
        {
            _myCondition = condition;
            _myWhenTrue = whenTrue;
            _myWhenFalse = whenFalse;

            if (!ReferenceEquals(_myCondition.ResultType, typeof(bool)))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.FirstArgNotBoolean,
                    CompileExceptionReason.TypeMismatch);
            }

            // The result type is the type that is common to the true/false operands
            if (ImplicitConverter.EmitImplicitConvert(_myWhenFalse.ResultType, _myWhenTrue.ResultType, null))
            {
                _myResultType = _myWhenTrue.ResultType;
            }
            else if (ImplicitConverter.EmitImplicitConvert(_myWhenTrue.ResultType, _myWhenFalse.ResultType, null))
            {
                _myResultType = _myWhenFalse.ResultType;
            }
            else
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.NeitherArgIsConvertibleToTheOther,
                    CompileExceptionReason.TypeMismatch,
                    _myWhenTrue.ResultType.Name,
                    _myWhenFalse.ResultType.Name);
            }
        }

        /// <summary>
        /// Delegates to <see cref="EmitConditional"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            EmitConditional(ilg, services);
        }

        /// <summary>
        /// Emits a branch-on-false to the false branch, the true branch with conversion to the
        /// chosen result type, an unconditional jump to the end, then the false branch with its
        /// own conversion.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitConditional(FleeILGenerator ilg, IServiceProvider services)
        {
            Label falseLabel = ilg.DefineLabel();
            Label endLabel = ilg.DefineLabel();

            // Emit the condition
            _myCondition.Emit(ilg, services);

            // On false go to the false operand
            ilg.EmitBranchFalse(falseLabel);

            // Emit the true operand
            _myWhenTrue.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(_myWhenTrue.ResultType, _myResultType, ilg);

            // Jump to end
            ilg.EmitBranch(endLabel);

            ilg.MarkLabel(falseLabel);

            // Emit the false operand
            _myWhenFalse.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(_myWhenFalse.ResultType, _myResultType, ilg);
            // Fall through to end
            ilg.MarkLabel(endLabel);
        }

        /// <summary>
        /// Gets the resolved result type.
        /// </summary>
        public override Type ResultType => _myResultType;
    }
}
