using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Literals.Integral;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Specifies the types of logical comparison operations supported by the Flee expression engine. Used to represent
    /// relational and equality operators in parsed expressions.
    /// </summary>
    internal enum LogicalCompareOperation
    {
        LessThan,
        GreaterThan,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual
    }

    /// <summary>
    /// Represents a comparison operation (such as ==, !=, &lt;, &gt;, &lt;=, &gt;=) within the Flee expression engine.
    /// Handles emitting IL for comparisons, including overloaded operators, string and boolean equality, and
    /// reference or enum comparisons.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CompareElement"/> class with the specified operands and operation.
    /// </remarks>
    /// <param name="leftChild">The left operand.</param>
    /// <param name="rightChild">The right operand.</param>
    /// <param name="operation">The comparison operation to perform.</param>
    internal class CompareElement(ExpressionElement leftChild, ExpressionElement rightChild,
        LogicalCompareOperation operation) : BinaryExpressionElement(leftChild, rightChild, operation)
    {
        /// <summary>
        /// Emits the IL for string equality or inequality, using the specified string comparison option.
        /// </summary>
        /// <param name="operation">The comparison operation.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private static void EmitStringEquality(LogicalCompareOperation operation, FleeILGenerator ilg,
            IServiceProvider services)
        {
            // Get the StringComparison from the options
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            Int32LiteralElement ic = new((int)options.StringComparison);

            ic.Emit(ilg, services);

            // and emit the method call
            MethodInfo mi = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string),
                typeof(StringComparison)], null);
            ilg.Emit(OpCodes.Call, mi);

            if (operation == LogicalCompareOperation.NotEqual)
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Ceq);
            }
        }

        /// <summary>
        /// Determines if the operation is equality or inequality.
        /// </summary>
        /// <param name="operation">The comparison operation.</param>
        /// <returns>True if the operation is equality or inequality; otherwise, false.</returns>
        private static bool IsOpTypeEqualOrNotEqual(LogicalCompareOperation operation)
        {
            return operation == LogicalCompareOperation.Equal || operation == LogicalCompareOperation.NotEqual;
        }

        /// <summary>
        /// Gets the OpCode for greater than or less than.
        /// </summary>
        /// <param name="greaterThan">True for greater than, false for less than.</param>
        /// <returns>The appropriate OpCode.</returns>
        private static OpCode GetCompareOpcode(bool greaterThan)
        {
            if (greaterThan)
            {
                return OpCodes.Cgt;
            }
            else
            {
                return OpCodes.Clt;
            }
        }

        /// <summary>
        /// Resolves the result type for the comparison operation, supporting overloaded operators, string and boolean
        /// equality, numeric comparisons, reference and enum comparisons.
        /// </summary>
        /// <param name="leftType">The type of the left operand.</param>
        /// <param name="rightType">The type of the right operand.</param>
        /// <returns>The result type, or null if the operation is not defined for the given types.</returns>
        protected override Type? ResolveResultType(Type leftType, Type rightType)
        {
            Type? binaryResultType = ImplicitConverter.GetBinaryResultType(leftType, rightType);
            MethodInfo? overloadedOperator = GetOverloadedCompareOperator();
            bool isEqualityOp = IsOpTypeEqualOrNotEqual((LogicalCompareOperation)_operation);

            // Use our string equality instead of overloaded operator
            if (ReferenceEquals(leftType, typeof(string)) && ReferenceEquals(rightType, typeof(string)) && isEqualityOp)
            {
                // String equality
                return typeof(bool);
            }
            else if (overloadedOperator != null)
            {
                return overloadedOperator.ReturnType;
            }
            else if (binaryResultType != null)
            {
                // Comparison of numeric operands
                return typeof(bool);
            }
            else if (ReferenceEquals(leftType, typeof(bool)) && ReferenceEquals(rightType, typeof(bool)) && isEqualityOp)
            {
                // Boolean equality
                return typeof(bool);
            }
            else if (AreBothChildrenReferenceTypes() && isEqualityOp)
            {
                // Comparison of reference types
                return typeof(bool);
            }
            else if (AreBothChildrenSameEnum())
            {
                return typeof(bool);
            }
            else
            {
                // Invalid operands
                return null;
            }
        }

        /// <summary>
        /// Attempts to resolve an overloaded operator method for the current comparison operation.
        /// </summary>
        /// <returns>The resolved <see cref="MethodInfo"/>, or null if not found.</returns>
        private MethodInfo? GetOverloadedCompareOperator()
        {
            string name = GetCompareOperatorName((LogicalCompareOperation)_operation);
            return GetOverloadedBinaryOperator(name, _operation);
        }

        /// <summary>
        /// Gets the name of the overloaded operator function for a given comparison operation.
        /// </summary>
        /// <param name="operation">The comparison operation.</param>
        /// <returns>The operator function name as a string.</returns>
        private static string GetCompareOperatorName(Enum operation)
        {
            return operation switch
            {
                LogicalCompareOperation.Equal => "Equality",
                LogicalCompareOperation.NotEqual => "Inequality",
                LogicalCompareOperation.GreaterThan => "GreaterThan",
                LogicalCompareOperation.LessThan => "LessThan",
                LogicalCompareOperation.GreaterThanOrEqual => "GreaterThanOrEqual",
                LogicalCompareOperation.LessThanOrEqual => "LessThanOrEqual",
                _ => throw new NotImplementedException(
                    $"Compare type {Enum.GetName(operation.GetType(), operation)} not implemented."),
            };
        }

        /// <summary>
        /// Emits the IL for a regular comparison (boolean, reference, or enum).
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private void EmitRegular(FleeILGenerator ilg, IServiceProvider services)
        {
            _leftChild.Emit(ilg, services);
            _rightChild.Emit(ilg, services);
            EmitCompareOperation((LogicalCompareOperation)_operation, ilg);
        }

        /// <summary>
        /// Determines if both children are reference types.
        /// </summary>
        /// <returns>True if both children are reference types; otherwise, false.</returns>
        private bool AreBothChildrenReferenceTypes() => !_leftChild.ResultType.IsValueType &&
            !_rightChild.ResultType.IsValueType;

        /// <summary>
        /// Determines if both children are the same enum type.
        /// </summary>
        /// <returns>True if both children are the same enum type; otherwise, false.</returns>
        private bool AreBothChildrenSameEnum() => _leftChild.ResultType.IsEnum &&
            ReferenceEquals(_leftChild.ResultType, _rightChild.ResultType);

        /// <summary>
        /// Emits the IL for the specified comparison operation.
        /// </summary>
        /// <param name="operation">The comparison operation.</param>
        /// <param name="ilg">The IL generator.</param>
        private void EmitCompareOperation(LogicalCompareOperation operation, FleeILGenerator ilg)
        {
            OpCode ltOpcode = GetCompareGTLTOpcode(false);
            OpCode gtOpcode = GetCompareGTLTOpcode(true);

            switch (operation)
            {
                case LogicalCompareOperation.Equal:
                    ilg.Emit(OpCodes.Ceq);
                    break;

                case LogicalCompareOperation.LessThan:
                    ilg.Emit(ltOpcode);
                    break;

                case LogicalCompareOperation.GreaterThan:
                    ilg.Emit(gtOpcode);
                    break;

                case LogicalCompareOperation.NotEqual:
                    ilg.Emit(OpCodes.Ceq);
                    ilg.Emit(OpCodes.Ldc_I4_0);
                    ilg.Emit(OpCodes.Ceq);
                    break;

                case LogicalCompareOperation.LessThanOrEqual:
                    ilg.Emit(gtOpcode);
                    ilg.Emit(OpCodes.Ldc_I4_0);
                    ilg.Emit(OpCodes.Ceq);
                    break;

                case LogicalCompareOperation.GreaterThanOrEqual:
                    ilg.Emit(ltOpcode);
                    ilg.Emit(OpCodes.Ldc_I4_0);
                    ilg.Emit(OpCodes.Ceq);
                    break;

                default:
                    throw new NotImplementedException(
                        $"Operation '{Enum.GetName(operation.GetType(), operation)}' not implemented.");
            }
        }

        /// <summary>
        /// Gets the correct greater/less than opcode for the comparison, considering unsigned types.
        /// </summary>
        /// <param name="greaterThan">True for greater than, false for less than.</param>
        /// <returns>The appropriate OpCode.</returns>
        private OpCode GetCompareGTLTOpcode(bool greaterThan)
        {
            Type leftType = _leftChild.ResultType;

            if (ReferenceEquals(leftType, _rightChild.ResultType))
            {
                if (ReferenceEquals(leftType, typeof(uint)) | ReferenceEquals(leftType, typeof(ulong)))
                {
                    if (greaterThan)
                    {
                        return OpCodes.Cgt_Un;
                    }
                    else
                    {
                        return OpCodes.Clt_Un;
                    }
                }
                else
                {
                    return GetCompareOpcode(greaterThan);
                }
            }
            else
            {
                return GetCompareOpcode(greaterThan);
            }
        }

        /// <summary>
        /// Emits the IL code for this comparison expression element, handling overloaded operators, string and boolean
        /// equality, numeric, reference, and enum comparisons.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type? binaryResultType = ImplicitConverter.GetBinaryResultType(_leftChild.ResultType, _rightChild.ResultType);
            MethodInfo? overloadedOperator = GetOverloadedCompareOperator();

            if (AreBothChildrenOfType(typeof(string)))
            {
                // String equality
                _leftChild.Emit(ilg, services);
                _rightChild.Emit(ilg, services);
                EmitStringEquality((LogicalCompareOperation)_operation, ilg, services);
            }
            else if (overloadedOperator != null)
            {
                EmitOverloadedOperatorCall(overloadedOperator, ilg, services);
            }
            else if (binaryResultType != null)
            {
                // Emit a compare of numeric operands
                EmitChildWithConvert(_leftChild, binaryResultType, ilg, services);
                EmitChildWithConvert(_rightChild, binaryResultType, ilg, services);
                EmitCompareOperation((LogicalCompareOperation)_operation, ilg);
            }
            else if (AreBothChildrenOfType(typeof(bool)))
            {
                // Boolean equality
                EmitRegular(ilg, services);
            }
            else if (AreBothChildrenReferenceTypes())
            {
                // Reference equality
                EmitRegular(ilg, services);
            }
            else if (_leftChild.ResultType.IsEnum && _rightChild.ResultType.IsEnum)
            {
                EmitRegular(ilg, services);
            }
            else
            {
                throw new InvalidOperationException("Unknown operand types for comparison.");
            }
        }
    }
}
