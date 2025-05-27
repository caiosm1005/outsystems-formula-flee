using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.LogicalBitwise
{
    /// <summary>
    /// Specifies the types of XOR operations supported by the Flee expression engine. Used to represent logical and
    /// bitwise XOR operators in parsed expressions.
    /// </summary>
    internal enum XorOperation
    {
        Xor
    }

    /// <summary>
    /// Represents a logical or bitwise XOR operation within the Flee expression engine.
    /// Supports both boolean XOR and bitwise XOR for integral types.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="XorElement"/> class with the specified operands and operation.
    /// </remarks>
    /// <param name="leftChild">The left operand.</param>
    /// <param name="rightChild">The right operand.</param>
    /// <param name="operation">The XOR operation to perform.</param>
    internal class XorElement(ExpressionElement leftChild, ExpressionElement rightChild, XorOperation operation) :
        BinaryExpressionElement(leftChild, rightChild, operation)
    {
        /// <summary>
        /// Resolves the result type for the XOR operation, supporting both bitwise and logical (boolean) XOR.
        /// </summary>
        /// <param name="leftType">The type of the left operand.</param>
        /// <param name="rightType">The type of the right operand.</param>
        /// <returns>The result type, or null if the operation is not defined for the given types.</returns>
        protected override Type? ResolveResultType(Type leftType, Type rightType)
        {
            Type? bitwiseType = Utility.GetBitwiseOpType(leftType, rightType);

            if (bitwiseType != null)
            {
                return bitwiseType;
            }
            else if (AreBothChildrenOfType(typeof(bool)))
            {
                return typeof(bool);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Emits the IL code for this XOR expression element, handling both bitwise and logical XOR.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _leftChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_leftChild.ResultType, ResultType, ilg);
            _rightChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_rightChild.ResultType, ResultType, ilg);
            ilg.Emit(OpCodes.Xor);
        }
    }
}
