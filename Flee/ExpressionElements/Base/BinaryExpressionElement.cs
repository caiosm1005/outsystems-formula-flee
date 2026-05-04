using System.Collections;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    /// <summary>
    /// Base class for expression elements that operate on two child elements (binary operators
    /// such as arithmetic, comparison, and bitwise ops).
    /// </summary>
    internal abstract class BinaryExpressionElement : ExpressionElement
    {
        /// <summary>
        /// The left operand element.
        /// </summary>
        protected ExpressionElement MyLeftChild = null!;

        /// <summary>
        /// The right operand element.
        /// </summary>
        protected ExpressionElement MyRightChild = null!;

        private Type? _myResultType;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        protected BinaryExpressionElement()
        {
        }

        /// <summary>
        /// Converts a flat list of operand/operator/operand/... values from the parser into a
        /// left-leaning binary tree of <paramref name="elementType"/> instances.
        /// </summary>
        /// <param name="childValues">The flat operand/operator list.</param>
        /// <param name="elementType">The CLR type of the binary element to create at each node.</param>
        /// <returns>The root of the constructed binary tree.</returns>
        public static BinaryExpressionElement CreateElement(IList childValues, Type elementType)
        {
            BinaryExpressionElement firstElement = (BinaryExpressionElement)Activator.CreateInstance(elementType)!;
            firstElement.Configure(
                (ExpressionElement)childValues[0]!,
                (ExpressionElement)childValues[2]!,
                childValues[1]!);

            BinaryExpressionElement lastElement = firstElement;

            for (int i = 3; i <= childValues.Count - 1; i += 2)
            {
                BinaryExpressionElement element = (BinaryExpressionElement)Activator.CreateInstance(elementType)!;
                element.Configure(lastElement, (ExpressionElement)childValues[i + 1]!, childValues[i]!);
                lastElement = element;
            }

            return lastElement;
        }

        /// <summary>
        /// Stores the operator value parsed from the source so this element knows which
        /// operation to emit. Implementation-specific.
        /// </summary>
        /// <param name="operation">The operator value from the parser.</param>
        protected abstract void GetOperation(object operation);

        /// <summary>
        /// Resolves the result type from the operand types and throws when the combination
        /// isn't supported.
        /// </summary>
        /// <param name="op">The operator value (used in the error message).</param>
        protected void ValidateInternal(object op)
        {
            _myResultType = GetResultType(MyLeftChild.ResultType, MyRightChild.ResultType);

            if (_myResultType == null)
            {
                ThrowOperandTypeMismatch(op, MyLeftChild.ResultType, MyRightChild.ResultType);
            }
        }

        /// <summary>
        /// Looks for an overloaded binary operator named <paramref name="name"/> on the operand
        /// types. Throws when ambiguous.
        /// </summary>
        /// <param name="name">The operator name (without the <c>op_</c> prefix).</param>
        /// <param name="operation">The operator value (used in the error message).</param>
        /// <returns>The chosen method, or <see langword="null"/> when no operator is defined.</returns>
        protected MethodInfo? GetOverloadedBinaryOperator(string name, object operation)
        {
            Type leftType = MyLeftChild.ResultType;
            Type rightType = MyRightChild.ResultType;
            BinaryOperatorBinder binder = new(leftType, rightType);

            // If both arguments are of the same type, pick either as the owner type
            if (ReferenceEquals(leftType, rightType))
            {
                return Utility.GetOverloadedOperator(name, leftType, binder, leftType, rightType);
            }

            // Get the operator for both types
            MethodInfo? leftMethod = Utility.GetOverloadedOperator(name, leftType, binder, leftType, rightType);
            MethodInfo? rightMethod = Utility.GetOverloadedOperator(name, rightType, binder, leftType, rightType);

            // Pick the right one
            if (leftMethod == null & rightMethod == null)
            {
                // No operator defined for either
                return null;
            }
            else if (leftMethod == null)
            {
                return rightMethod;
            }
            else if (rightMethod == null)
            {
                return leftMethod;
            }
            else if (ReferenceEquals(leftMethod, rightMethod))
            {
                // same operator for both (most likely defined in a common base class)
                return leftMethod;
            }
            else
            {
                // Ambiguous call
                ThrowAmbiguousCallException(leftType, rightType, operation);
                return null;
            }
        }

        /// <summary>
        /// Emits a call to <paramref name="method"/>, evaluating both children with implicit
        /// conversions to the parameter types.
        /// </summary>
        /// <param name="method">The operator method to call.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        protected void EmitOverloadedOperatorCall(MethodInfo method, FleeILGenerator ilg, IServiceProvider services)
        {
            ParameterInfo[] @params = method.GetParameters();
            ParameterInfo pLeft = @params[0];
            ParameterInfo pRight = @params[1];

            EmitChildWithConvert(MyLeftChild, pLeft.ParameterType, ilg, services);
            EmitChildWithConvert(MyRightChild, pRight.ParameterType, ilg, services);
            ilg.Emit(OpCodes.Call, method);
        }

        /// <summary>
        /// Throws a "operation not defined for types" compile exception.
        /// </summary>
        /// <param name="operation">The operator value.</param>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        protected void ThrowOperandTypeMismatch(object operation, Type leftType, Type rightType)
        {
            ThrowCompileException(
                CompileErrorResourceKeys.OperationNotDefinedForTypes,
                CompileExceptionReason.TypeMismatch,
                operation,
                leftType.Name,
                rightType.Name);
        }

        /// <summary>
        /// Computes the result type of this operation given the operand types, returning
        /// <see langword="null"/> when the combination isn't supported.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <returns>The result type, or <see langword="null"/>.</returns>
        protected abstract Type? GetResultType(Type leftType, Type rightType);

        /// <summary>
        /// Emits a child element followed by an implicit conversion to <paramref name="resultType"/>.
        /// Asserts the conversion succeeded — operand validation runs before emit.
        /// </summary>
        /// <param name="child">The child element.</param>
        /// <param name="resultType">The desired result type.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        protected static void EmitChildWithConvert(
            ExpressionElement child,
            Type resultType,
            FleeILGenerator ilg,
            IServiceProvider services)
        {
            child.Emit(ilg, services);
            bool converted = ImplicitConverter.EmitImplicitConvert(child.ResultType, resultType, ilg);
            Debug.Assert(converted, "convert failed");
        }

        /// <summary>
        /// Returns whether both children evaluate to <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target type.</param>
        /// <returns><see langword="true"/> when both match.</returns>
        protected bool AreBothChildrenOfType(Type target)
        {
            return IsChildOfType(MyLeftChild, target) & IsChildOfType(MyRightChild, target);
        }

        /// <summary>
        /// Returns whether either child evaluates to <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target type.</param>
        /// <returns><see langword="true"/> when at least one matches.</returns>
        protected bool IsEitherChildOfType(Type target)
        {
            return IsChildOfType(MyLeftChild, target) || IsChildOfType(MyRightChild, target);
        }

        /// <summary>
        /// Returns whether <paramref name="child"/>'s result type is reference-equal to
        /// <paramref name="t"/>.
        /// </summary>
        /// <param name="child">The child element.</param>
        /// <param name="t">The target type.</param>
        /// <returns><see langword="true"/> when the types match.</returns>
        protected static bool IsChildOfType(ExpressionElement child, Type t)
        {
            return ReferenceEquals(child.ResultType, t);
        }

        /// <summary>
        /// Sets the left and right operands, captures the operator, and resolves the result type.
        /// </summary>
        /// <param name="leftChild">The left operand.</param>
        /// <param name="rightChild">The right operand.</param>
        /// <param name="op">The operator value from the parser.</param>
        private void Configure(ExpressionElement leftChild, ExpressionElement rightChild, object op)
        {
            MyLeftChild = leftChild;
            MyRightChild = rightChild;
            GetOperation(op);

            ValidateInternal(op);
        }

        /// <summary>
        /// Gets the resolved result type of this binary operation. Set during <see cref="Configure"/>.
        /// </summary>
        public sealed override Type ResultType => _myResultType!;
    }
}
