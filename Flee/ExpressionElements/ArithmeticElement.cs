using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Binary arithmetic element. Handles user-defined operator overloads, primitive
    /// arithmetic (with optional checked-overflow), and <c>+</c> as string concatenation.
    /// </summary>
    internal class ArithmeticElement : BinaryExpressionElement
    {
        private static MethodInfo _ourStringConcatMethodInfo = null!;
        private static MethodInfo _ourObjectConcatMethodInfo = null!;
        private BinaryArithmeticOperation _myOperation;

        /// <summary>
        /// Initializes a new instance and caches the well-known concat methods.
        /// </summary>
        public ArithmeticElement()
        {
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

            if (overloadedMethod != null)
            {
                return overloadedMethod.ReturnType;
            }
            else if (binaryResultType != null)
            {
                return binaryResultType;
            }
            else if (IsEitherChildOfType(typeof(string)) & (_myOperation == BinaryArithmeticOperation.Add))
            {
                return typeof(string);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Looks up the overloaded operator method matching <see cref="_myOperation"/>.
        /// </summary>
        /// <returns>The chosen method, or <see langword="null"/> when none is defined.</returns>
        private MethodInfo? GetOverloadedArithmeticOperator()
        {
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
                EmitOverloadedOperatorCall(overloadedMethod, ilg, services);
            }
            else if (IsEitherChildOfType(typeof(string)))
            {
                EmitStringConcat(ilg, services);
            }
            else
            {
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
            EmitChildWithConvert(MyRightChild, ResultType, ilg, services);

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
                default:
                    Debug.Fail("Unknown op type");
                    break;
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

            MyLeftChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, argType, ilg);
            MyRightChild.Emit(ilg, services);
            _ = ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, argType, ilg);
            ilg.Emit(OpCodes.Call, concatMethodInfo);
        }
    }
}
