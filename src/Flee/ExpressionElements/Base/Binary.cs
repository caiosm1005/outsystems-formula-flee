using System.Collections;
using System.Reflection.Emit;
using System.Reflection;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    /// <summary>
    /// Represents the abstract base class for expression elements that operate on two child elements. Provides common
    /// logic for binary operations, including operator resolution, type validation, and emitting IL for overloaded
    /// operators.
    /// </summary>
    internal abstract class BinaryExpressionElement : ExpressionElement
    {
        /// <summary>
        /// The enum value representing the specific binary operation (e.g., Add, Subtract).
        /// </summary>
        protected readonly Enum _operation;

        /// <summary>
        /// Converts a flat list of child values into a binary tree of <see cref="BinaryExpressionElement"/>s.
        /// </summary>
        /// <typeparam name="T">The concrete type of <see cref="BinaryExpressionElement"/> to create.</typeparam>
        /// <param name="childValues">A list containing alternating expression elements and operator enums.</param>
        /// <returns>The root <typeparamref name="T"/> of the constructed binary tree.</returns>
        public static T CreateFromChildValues<T>(IList childValues) where T : BinaryExpressionElement
        {
            var firstElement = (T)Activator.CreateInstance(typeof(T), (ExpressionElement)childValues[0],
                (ExpressionElement)childValues[2], childValues[1]);

            T lastElement = firstElement;

            for (int i = 3; i <= childValues.Count - 1; i += 2)
            {
                lastElement = (T)Activator.CreateInstance(typeof(T), lastElement, (ExpressionElement)childValues[i + 1],
                    childValues[i]);
            }

            return lastElement;
        }

        /// <summary>
        /// Emits the IL for a child element, converting its type to the required result type if necessary.
        /// Throws an exception if the conversion is not possible.
        /// </summary>
        /// <param name="child">The child expression element.</param>
        /// <param name="resultType">The required result type.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        protected static void EmitChildWithConvert(ExpressionElement child, Type resultType, FleeILGenerator ilg,
            IServiceProvider services)
        {
            child.Emit(ilg, services);
            bool converted = ImplicitConverter.EmitImplicitConvert(child.ResultType, resultType, ilg);
            
            if (!converted)
            {
                throw new InvalidOperationException("Implicit conversion failed.");
            }
        }

        /// <summary>
        /// The result type of the binary expression, determined by the types of the child elements.
        /// </summary>
        private readonly Type _resultType;

        /// <summary>
        /// The left child of the binary expression.
        /// </summary>
        protected readonly ExpressionElement _leftChild;

        /// <summary>
        /// The right child of the binary expression.
        /// </summary>
        protected readonly ExpressionElement _rightChild;

        /// <summary>
        /// Resolves the result type for the binary operation, given the types of the left and right children.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="leftType">The type of the left child.</param>
        /// <param name="rightType">The type of the right child.</param>
        /// <returns>The result type, or null if the operation is not defined for the given types.</returns>
        protected abstract Type? ResolveResultType(Type leftType, Type rightType);

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryExpressionElement"/> class.
        /// Validates and sets the result type based on the child types and operation.
        /// </summary>
        /// <param name="leftChild">The left child expression element.</param>
        /// <param name="rightChild">The right child expression element.</param>
        /// <param name="operation">The enum value representing the operation.</param>
        /// <exception cref="ExpressionCompileException">
        /// Thrown if the operation is not defined for the given child types.
        /// </exception>
        public BinaryExpressionElement(ExpressionElement leftChild, ExpressionElement rightChild, Enum operation)
        {
            _leftChild = leftChild;
            _rightChild = rightChild;
            _operation = operation;

            // Set result type (with validation)
            _resultType = ResolveResultType(leftChild.ResultType, rightChild.ResultType) ??
                throw new ExpressionCompileException(Name, CompileErrorResourceKeys.OperationNotDefinedForTypes,
                    CompileExceptionReason.TypeMismatch, operation, leftChild.ResultType.Name,
                    rightChild.ResultType.Name);
        }

        /// <summary>
        /// Attempts to resolve an overloaded binary operator method for the given operation name and argument types.
        /// </summary>
        /// <param name="name">The operator method name (e.g., "op_Addition").</param>
        /// <param name="operation">The enum value representing the operation.</param>
        /// <returns>The resolved <see cref="MethodInfo"/>, or null if not found.</returns>
        /// <exception cref="ExpressionCompileException">
        /// Thrown if the operator is ambiguous for the given types.
        /// </exception>
        protected MethodInfo? GetOverloadedBinaryOperator(string name, Enum operation)
        {
            Type leftType = _leftChild.ResultType;
            Type rightType = _rightChild.ResultType;
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
            if (leftMethod == null && rightMethod == null)
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
                throw new ExpressionCompileException(Name, CompileErrorResourceKeys.AmbiguousOverloadedOperator,
                    CompileExceptionReason.AmbiguousMatch, leftType.Name, rightType.Name, operation);
            }
        }

        /// <summary>
        /// Emits IL to call an overloaded operator method for this binary expression.
        /// </summary>
        /// <param name="method">The operator method to call.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        protected void EmitOverloadedOperatorCall(MethodInfo method, FleeILGenerator ilg, IServiceProvider services)
        {
            ParameterInfo[] @params = method.GetParameters();
            ParameterInfo pLeft = @params[0];
            ParameterInfo pRight = @params[1];

            EmitChildWithConvert(_leftChild, pLeft.ParameterType, ilg, services);
            EmitChildWithConvert(_rightChild, pRight.ParameterType, ilg, services);
            ilg.Emit(OpCodes.Call, method);
        }

        /// <summary>
        /// Determines if both child elements have the specified target type.
        /// </summary>
        /// <param name="target">The target type to check.</param>
        /// <returns>True if both children have the target type; otherwise, false.</returns>
        protected bool AreBothChildrenOfType(Type target) => IsChildOfType(_leftChild, target) &&
            IsChildOfType(_rightChild, target);

        /// <summary>
        /// Determines if either child element has the specified target type.
        /// </summary>
        /// <param name="target">The target type to check.</param>
        /// <returns>True if either child has the target type; otherwise, false.</returns>
        protected bool IsEitherChildOfType(Type target) => IsChildOfType(_leftChild, target) ||
            IsChildOfType(_rightChild, target);

        /// <summary>
        /// Determines if the specified child element has the given type.
        /// </summary>
        /// <param name="child">The child expression element.</param>
        /// <param name="t">The type to check.</param>
        /// <returns>True if the child has the specified type; otherwise, false.</returns>
        protected static bool IsChildOfType(ExpressionElement child, Type t) => ReferenceEquals(child.ResultType, t);

        /// <summary>
        /// Gets the result type of this binary expression.
        /// </summary>
        public sealed override Type ResultType => _resultType;
    }
}
