using System.Diagnostics;
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
    /// Binary arithmetic element. Handles user-defined operator overloads, primitive
    /// arithmetic (with optional checked-overflow), <c>+</c> as string concatenation, and
    /// optimized integer-exponent power evaluation.
    /// </summary>
    internal class ArithmeticElement : BinaryExpressionElement
    {
        private static MethodInfo _ourPowerMethodInfo = null!;
        private static MethodInfo _ourStringConcatMethodInfo = null!;
        private static MethodInfo _ourObjectConcatMethodInfo = null!;
        private BinaryArithmeticOperation _myOperation;

        /// <summary>
        /// Initializes a new instance and caches the well-known concat/Math.Pow methods.
        /// </summary>
        public ArithmeticElement()
        {
            _ourPowerMethodInfo = typeof(Math).GetMethod("Pow", BindingFlags.Public | BindingFlags.Static)!;
            _ourStringConcatMethodInfo = typeof(string).GetMethod(
                "Concat",
                [typeof(string), typeof(string)],
                null)!;
            _ourObjectConcatMethodInfo = typeof(string).GetMethod(
                "Concat",
                [typeof(object), typeof(object)],
                null)!;
        }

        /// <summary>
        /// Stores the parsed <see cref="BinaryArithmeticOperation"/>.
        /// </summary>
        /// <param name="operation">The operator value from the parser.</param>
        protected override void GetOperation(object operation)
        {
            _myOperation = (BinaryArithmeticOperation)operation;
        }

        /// <summary>
        /// Resolves the result type by checking, in order: user-defined operator overload,
        /// primitive binary result, then <c>+</c>-as-concatenation when one operand is a string.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <returns>The result type, or <see langword="null"/> when not supported.</returns>
        protected override Type? GetResultType(Type leftType, Type rightType)
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
                // Operands are primitive types.  Return computed result type unless we are doing
                // a power operation
                return _myOperation == BinaryArithmeticOperation.Power
                    ? GetPowerResultType(leftType)
                    : binaryResultType;
            }
            else if (IsEitherChildOfType(typeof(string)) & (_myOperation == BinaryArithmeticOperation.Add))
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
        /// Returns the result type of <c>x ^ n</c>: the operand type when the exponent is a
        /// non-negative <see cref="int"/> literal, otherwise <see cref="double"/>.
        /// </summary>
        /// <param name="leftType">The base operand type.</param>
        /// <returns>The result type.</returns>
        private Type GetPowerResultType(Type leftType)
        {
            return IsOptimizablePower ? leftType : typeof(double);
        }

        /// <summary>
        /// Looks up the overloaded operator method matching <see cref="_myOperation"/>.
        /// </summary>
        /// <returns>The chosen method, or <see langword="null"/> when none is defined.</returns>
        private MethodInfo? GetOverloadedArithmeticOperator()
        {
            // Get the name of the operator
            string name = GetOverloadedOperatorFunctionName(_myOperation);
            return GetOverloadedBinaryOperator(name, _myOperation);
        }

        /// <summary>
        /// Maps a <see cref="BinaryArithmeticOperation"/> to its <c>op_</c> method-name suffix.
        /// </summary>
        /// <param name="op">The arithmetic operation.</param>
        /// <returns>The operator function name.</returns>
        private static string GetOverloadedOperatorFunctionName(BinaryArithmeticOperation op)
        {
            switch (op)
            {
                case BinaryArithmeticOperation.Add:
                    return "Addition";
                case BinaryArithmeticOperation.Subtract:
                    return "Subtraction";
                case BinaryArithmeticOperation.Multiply:
                    return "Multiply";
                case BinaryArithmeticOperation.Divide:
                    return "Division";
                case BinaryArithmeticOperation.Mod:
                    return "Modulus";
                case BinaryArithmeticOperation.Power:
                    return "Exponent";
                default:
                    Debug.Assert(false, "unknown operator type");
                    return string.Empty;
            }
        }

        /// <summary>
        /// Emits the operation: prefers user-defined operator overloads, then string concat,
        /// then primitive arithmetic.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
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
                EmitArithmeticOperation(_myOperation, ilg, services);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="t"/> is one of the unsigned integral primitives that
        /// we use for picking the unsigned arithmetic opcodes.
        /// </summary>
        /// <param name="t">The operand type.</param>
        /// <returns><see langword="true"/> when unsigned.</returns>
        private static bool IsUnsignedForArithmetic(Type t)
        {
            return ReferenceEquals(t, typeof(UInt32)) | ReferenceEquals(t, typeof(UInt64));
        }

        /// <summary>
        /// Emit an arithmetic operation with handling for unsigned and checked contexts.
        /// </summary>
        /// <param name="op">The arithmetic operation to emit.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitArithmeticOperation(
            BinaryArithmeticOperation op,
            FleeILGenerator ilg,
            IServiceProvider services)
        {
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions))!;
            bool unsigned = IsUnsignedForArithmetic(MyLeftChild.ResultType)
                & IsUnsignedForArithmetic(MyRightChild.ResultType);
            bool integral = Utility.IsIntegralType(MyLeftChild.ResultType)
                & Utility.IsIntegralType(MyRightChild.ResultType);
            bool emitOverflow = integral & options.Checked;

            EmitChildWithConvert(MyLeftChild, ResultType, ilg, services);

            if (!IsOptimizablePower)
            {
                EmitChildWithConvert(MyRightChild, ResultType, ilg, services);
            }

            switch (op)
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
                    Debug.Fail("Unknown op type");
                    break;
            }
        }

        /// <summary>
        /// Emits the power operator: a sequence of duplicates and multiplies for the optimizable
        /// case, otherwise a call to <see cref="Math.Pow"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="emitOverflow">Whether to emit checked-overflow opcodes.</param>
        /// <param name="unsigned">Whether the operands are unsigned.</param>
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
        /// Emits inline duplicate/multiply for an integer-exponent power. <c>x^0</c> short-circuits
        /// to a literal <c>1</c> typed to the operand type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="emitOverflow">Whether to emit checked-overflow opcodes.</param>
        /// <param name="unsigned">Whether the operands are unsigned.</param>
        private void EmitOptimizedPower(FleeILGenerator ilg, bool emitOverflow, bool unsigned)
        {
            Int32LiteralElement right = (Int32LiteralElement)MyRightChild;

            if (right.Value == 0)
            {
                ilg.Emit(OpCodes.Pop);
                LiteralElement.EmitLoad(1, ilg);
                _ = ImplicitConverter.EmitImplicitNumericConvert(typeof(Int32), MyLeftChild.ResultType, ilg);
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
        /// Emits the multiplication opcode, picking the checked/unsigned variant when configured.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="emitOverflow">Whether to emit checked-overflow opcodes.</param>
        /// <param name="unsigned">Whether the operands are unsigned.</param>
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
        /// Emit a string concatenation, picking <see cref="string.Concat(string, string)"/>
        /// when both operands are strings and the object overload otherwise.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
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
                Debug.Assert(IsEitherChildOfType(typeof(string)), "one child must be a string");
                concatMethodInfo = _ourObjectConcatMethodInfo;
                argType = typeof(object);
            }

            // Emit the operands and call the function
            MyLeftChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, argType, ilg);
            MyRightChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, argType, ilg);
            ilg.Emit(OpCodes.Call, concatMethodInfo);
        }

        /// <summary>
        /// Gets a value indicating whether the right operand is a non-negative <see cref="int"/>
        /// literal — the case where the inline duplicate/multiply optimization applies.
        /// </summary>
        private bool IsOptimizablePower
        {
            get
            {
                if (_myOperation != BinaryArithmeticOperation.Power || MyRightChild is not Int32LiteralElement)
                {
                    return false;
                }

                Int32LiteralElement right = (Int32LiteralElement)MyRightChild;

                return right?.Value >= 0;
            }
        }
    }
}
