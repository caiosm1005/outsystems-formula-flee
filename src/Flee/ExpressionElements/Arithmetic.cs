using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Base.Literals;
using Flee.ExpressionElements.Literals.Integral;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Represents a binary arithmetic operation element within the Flee expression engine.
    /// </summary>
    internal enum BinaryArithmeticOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Mod,
        Power
    }

    /// <summary>
    /// Represents a binary arithmetic operation (such as addition, subtraction, multiplication, division, modulus, or
    /// power) within the Flee expression engine. Handles emitting IL for arithmetic operations, including overflow
    /// checking, unsigned arithmetic, string concatenation, and operator overloading.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ArithmeticElement"/> class with the specified operands and
    /// operation.
    /// </remarks>
    /// <param name="leftChild">The left operand.</param>
    /// <param name="rightChild">The right operand.</param>
    /// <param name="operation">The arithmetic operation to perform.</param>
    internal class ArithmeticElement(ExpressionElement leftChild, ExpressionElement rightChild,
        BinaryArithmeticOperation operation) : BinaryExpressionElement(leftChild, rightChild, operation)
    {
        /// <summary>
        /// MethodInfo for Math.Pow, used for exponentiation.
        /// </summary>
        private static readonly MethodInfo _ourPowerMethodInfo = typeof(Math).GetMethod(nameof(Math.Pow),
            BindingFlags.Public | BindingFlags.Static);

        /// <summary>
        /// MethodInfo for string.Concat(string, string), used for string concatenation.
        /// </summary>
        private static readonly MethodInfo _ourStringConcatMethodInfo = typeof(string).GetMethod(nameof(string.Concat),
            [typeof(string), typeof(string)], null);

        /// <summary>
        /// MethodInfo for string.Concat(object, object), used for object concatenation.
        /// </summary>
        private static readonly MethodInfo _ourObjectConcatMethodInfo = typeof(string).GetMethod(nameof(string.Concat),
            [typeof(object), typeof(object)], null);

        /// <summary>
        /// Determines if the specified type is an unsigned integer type (uint or ulong).
        /// </summary>
        private static bool IsUnsignedForArithmetic(Type t)
        {
            return ReferenceEquals(t, typeof(uint)) || ReferenceEquals(t, typeof(ulong));
        }

        /// <summary>
        /// Gets the name of the overloaded operator function for a given arithmetic operation.
        /// </summary>
        /// <param name="operation">The binary arithmetic operation.</param>
        /// <returns>The operator function name as a string.</returns>
        private static string GetOverloadedOperatorFunctionName(BinaryArithmeticOperation operation)
        {
            return operation switch
            {
                BinaryArithmeticOperation.Add => "Addition",
                BinaryArithmeticOperation.Subtract => "Subtraction",
                BinaryArithmeticOperation.Multiply => "Multiply",
                BinaryArithmeticOperation.Divide => "Division",
                BinaryArithmeticOperation.Mod => "Modulus",
                BinaryArithmeticOperation.Power => "Exponent",
                _ => throw new NotImplementedException($"Binary arithmetic operation" +
                    $" {Enum.GetName(operation.GetType(), operation)} not implemented."),
            };
        }

        /// <summary>
        /// Determines if the power operation can be optimized (i.e., right operand is a non-negative integer literal).
        /// </summary>
        private bool IsOptimizablePower
        {
            get
            {
                if ((BinaryArithmeticOperation)_operation != BinaryArithmeticOperation.Power ||
                    _rightChild is not Int32LiteralElement)
                {
                    return false;
                }

                var rightChild = (Int32LiteralElement)_rightChild;

                return rightChild.Value >= 0;
            }
        }

        /// <summary>
        /// Gets the result type for the power operation, which is either the left type (if optimizable) or double.
        /// </summary>
        /// <param name="leftType">The type of the left operand.</param>
        /// <returns>The result type for the power operation.</returns>
        private Type GetPowerResultType(Type leftType) => IsOptimizablePower ? leftType : typeof(double);

        /// <summary>
        /// Attempts to resolve an overloaded operator method for the current arithmetic operation.
        /// </summary>
        /// <returns>The resolved MethodInfo, or null if not found.</returns>
        private MethodInfo? GetOverloadedArithmeticOperator()
        {
            // Get the name of the operator
            string name = GetOverloadedOperatorFunctionName((BinaryArithmeticOperation)_operation);
            return GetOverloadedBinaryOperator(name, _operation);
        }

        /// <summary>
        /// Emits IL for an arithmetic operation, handling unsigned and checked contexts.
        /// </summary>
        /// <param name="operation">The arithmetic operation to emit.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private void EmitArithmeticOperation(BinaryArithmeticOperation operation, FleeILGenerator ilg,
            IServiceProvider services)
        {
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));

            bool unsigned = IsUnsignedForArithmetic(_leftChild.ResultType) &&
                IsUnsignedForArithmetic(_rightChild.ResultType);
            bool integral = Utility.IsIntegralType(_leftChild.ResultType) &&
                Utility.IsIntegralType(_rightChild.ResultType);
            bool emitOverflow = integral && options.Checked;

            EmitChildWithConvert(_leftChild, ResultType, ilg, services);

            if (!IsOptimizablePower)
            {
                EmitChildWithConvert(_rightChild, ResultType, ilg, services);
            }

            switch (operation)
            {
                case BinaryArithmeticOperation.Add:
                    if (emitOverflow)
                    {
                        if (unsigned)
                        {
                            ilg.Emit(OpCodes.Add_Ovf_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Add_Ovf);
                        }
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Add);
                    }
                    break;

                case BinaryArithmeticOperation.Subtract:
                    if (emitOverflow)
                    {
                        if (unsigned)
                        {
                            ilg.Emit(OpCodes.Sub_Ovf_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Sub_Ovf);
                        }
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Sub);
                    }
                    break;

                case BinaryArithmeticOperation.Multiply:
                    EmitMultiply(ilg, emitOverflow, unsigned);
                    break;

                case BinaryArithmeticOperation.Divide:
                    if (unsigned)
                    {
                        ilg.Emit(OpCodes.Div_Un);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Div);
                    }
                    break;

                case BinaryArithmeticOperation.Mod:
                    if (unsigned)
                    {
                        ilg.Emit(OpCodes.Rem_Un);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Rem);
                    }
                    break;

                case BinaryArithmeticOperation.Power:
                    EmitPower(ilg, emitOverflow, unsigned);
                    break;

                default:
                    throw new NotImplementedException("Arithmetic operation" +
                        $" '{Enum.GetName(operation.GetType(), operation)}' is not implemented.");
            }
        }

        /// <summary>
        /// Emits IL for the power operation, using an optimized approach if possible.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="emitOverflow">Whether to check for overflow.</param>
        /// <param name="unsigned">Whether the operation is unsigned.</param>
        private void EmitPower(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
        {
            if (IsOptimizablePower)
            {
                EmitOptimizedPower(ilg, emitOverflow, unsigned);
            }
            else
            {
                ilg.Emit(OpCodes.Call, _ourPowerMethodInfo);
            }
        }

        /// <summary>
        /// Emits IL for an optimized integer power operation (when the exponent is a non-negative integer literal).
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="emitOverflow">Whether to check for overflow.</param>
        /// <param name="unsigned">Whether the operation is unsigned.</param>
        private void EmitOptimizedPower(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
        {
            Int32LiteralElement right = (Int32LiteralElement)_rightChild;

            if (right.Value == 0)
            {
                ilg.Emit(OpCodes.Pop);
                LiteralElement.EmitLoad(1, ilg);
                ImplicitConverter.EmitImplicitNumericConvert(typeof(int), _leftChild.ResultType, ilg);
                return;
            }

            if (right.Value == 1)
            {
                return;
            }

            // Start at 1 since left operand has already been emited once
            for (int i = 1; i <= right.Value - 1; i++)
            {
                ilg.Emit(OpCodes.Dup);
            }

            for (int i = 1; i <= right.Value - 1; i++)
            {
                EmitMultiply(ilg, emitOverflow, unsigned);
            }
        }

        /// <summary>
        /// Emits IL for a multiplication operation, handling overflow and unsigned contexts.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="emitOverflow">Whether to check for overflow.</param>
        /// <param name="unsigned">Whether the operation is unsigned.</param>
        private void EmitMultiply(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
        {
            if (emitOverflow)
            {
                if (unsigned)
                {
                    ilg.Emit(OpCodes.Mul_Ovf_Un);
                }
                else
                {
                    ilg.Emit(OpCodes.Mul_Ovf);
                }
            }
            else
            {
                ilg.Emit(OpCodes.Mul);
            }
        }

        /// <summary>
        /// Emits IL for string concatenation using the appropriate overload of string.Concat.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private void EmitStringConcat(FleeILGenerator ilg, IServiceProvider services)
        {
            Type argType;
            MethodInfo concatMethodInfo;

            // Pick the most specific concat method
            if (AreBothChildrenOfType(typeof(string)))
            {
                concatMethodInfo = _ourStringConcatMethodInfo;
                argType = typeof(string);
            }
            else
            {
                if (!IsEitherChildOfType(typeof(string)))
                {
                    throw new InvalidOperationException("One child must be a string.");
                }
                concatMethodInfo = _ourObjectConcatMethodInfo;
                argType = typeof(object);
            }

            // Emit the operands and call the function
            _leftChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_leftChild.ResultType, argType, ilg);
            _rightChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_rightChild.ResultType, argType, ilg);
            ilg.Emit(OpCodes.Call, concatMethodInfo);
        }

        /// <summary>
        /// Resolves the result type for the arithmetic operation, considering overloaded operators, primitive types,
        /// and string concatenation.
        /// </summary>
        /// <param name="leftType">The type of the left operand.</param>
        /// <param name="rightType">The type of the right operand.</param>
        /// <returns>The result type, or null if the operation is not defined for the given types.</returns>
        protected override Type? ResolveResultType(Type leftType, Type rightType)
        {
            Type? binaryResultType = ImplicitConverter.GetBinaryResultType(leftType, rightType);
            MethodInfo? overloadedMethod = GetOverloadedArithmeticOperator();

            // Is an overloaded operator defined for our left and right children?
            if (overloadedMethod != null)
            {
                // Yes, so use its return type
                return overloadedMethod.ReturnType;
            }
            else if (binaryResultType != null)
            {
                // Operands are primitive types.  Return computed result type unless we are doing a power operation
                if ((BinaryArithmeticOperation)_operation == BinaryArithmeticOperation.Power)
                {
                    return GetPowerResultType(leftType);
                }
                else
                {
                    return binaryResultType;
                }
            }
            else if (IsEitherChildOfType(typeof(string)) &&
                (BinaryArithmeticOperation)_operation == BinaryArithmeticOperation.Add)
            {
                // String concatenation
                return typeof(string);
            }
            else
            {
                // Invalid types
                return null;
            }
        }

        /// <summary>
        /// Emits the IL code for this arithmetic expression element, handling overloaded operators, string
        /// concatenation, and standard arithmetic operations.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            MethodInfo? overloadedMethod = GetOverloadedArithmeticOperator();

            if (overloadedMethod != null)
            {
                // Emit a call to an overloaded operator
                EmitOverloadedOperatorCall(overloadedMethod, ilg, services);
            }
            else if (IsEitherChildOfType(typeof(string)))
            {
                // One of our operands is a string so emit a concatenation
                EmitStringConcat(ilg, services);
            }
            else
            {
                // Emit a regular arithmetic operation			
                EmitArithmeticOperation((BinaryArithmeticOperation)_operation, ilg, services);
            }
        }
    }
}
