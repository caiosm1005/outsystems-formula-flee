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
    internal class ArithmeticElement : BinaryExpressionElement
    {
        private static MethodInfo _ourPowerMethodInfo;
        private static MethodInfo _ourStringConcatMethodInfo;
        private BinaryArithmeticOperation _myOperation;

        public ArithmeticElement()
        {
            _ourPowerMethodInfo = typeof(Math).GetMethod("Pow", BindingFlags.Public | BindingFlags.Static);
            _ourStringConcatMethodInfo = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }, null);
        }

        public static string CustomConcat(object left, object right)
        {
            return string.Concat(left, right);
        }

        protected override void GetOperation(object operation)
        {
            _myOperation = (BinaryArithmeticOperation)operation;
        }

        protected override Type GetResultType(Type leftType, Type rightType)
        {
            Type binaryResultType = ImplicitConverter.GetBinaryResultType(leftType, rightType);
            MethodInfo overloadedMethod = GetOverloadedArithmeticOperator();

            // Is an overloaded operator defined for our left and right children?
            if (overloadedMethod != null)
            {
                // Yes, so use its return type
                return overloadedMethod.ReturnType;
            }
            else if (binaryResultType != null)
            {
                // Operands are primitive types.  Return computed result type unless we are doing a power operation
                if (_myOperation == BinaryArithmeticOperation.Power)
                {
                    return GetPowerResultType(leftType, rightType, binaryResultType);
                }
                else
                {
                    return binaryResultType;
                }
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

        private Type GetPowerResultType(Type leftType, Type rightType, Type binaryResultType)
        {
            if (IsOptimizablePower)
            {
                return leftType;
            }
            else
            {
                return typeof(double);
            }
        }

        private MethodInfo GetOverloadedArithmeticOperator()
        {
            // Get the name of the operator
            string name = GetOverloadedOperatorFunctionName(_myOperation);
            return GetOverloadedBinaryOperator(name, _myOperation);
        }

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
                    return null;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            MethodInfo overloadedMethod = GetOverloadedArithmeticOperator();

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

        private static bool IsUnsignedForArithmetic(Type t)
        {
            return ReferenceEquals(t, typeof(uint)) | ReferenceEquals(t, typeof(ulong));
        }

        /// <summary>
        /// Emit an arithmetic operation with handling for unsigned and checked contexts
        /// </summary>
        /// <param name="op"></param>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        private void EmitArithmeticOperation(BinaryArithmeticOperation op, FleeILGenerator ilg, IServiceProvider services)
        {
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            bool unsigned = IsUnsignedForArithmetic(MyLeftChild.ResultType) & IsUnsignedForArithmetic(MyRightChild.ResultType);
            bool integral = Utility.IsIntegralType(MyLeftChild.ResultType) & Utility.IsIntegralType(MyRightChild.ResultType);
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
                    EmitMultiply(emitOverflow, unsigned, ilg);
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
                    EmitPower(emitOverflow, unsigned, ilg, services);
                    break;
                default:
                    Debug.Fail("Unknown op type");
                    break;
            }
        }

        private void EmitPower(bool emitOverflow, bool unsigned, FleeILGenerator ilg, IServiceProvider services)
        {
            if (IsOptimizablePower)
            {
                EmitOptimizedPower(emitOverflow, unsigned, ilg, services);
            }
            else
            {
                ilg.Emit(OpCodes.Call, _ourPowerMethodInfo);
            }
        }

        private void EmitOptimizedPower(bool emitOverflow, bool unsigned, FleeILGenerator ilg, IServiceProvider services)
        {
            Int32LiteralElement right = (Int32LiteralElement)MyRightChild;

            if (right.Value == 0)
            {
                ilg.Emit(OpCodes.Pop);
                LiteralElement.EmitLoad(1, ilg);
                ImplicitConverter.EmitImplicitNumericConvert(typeof(int), MyLeftChild.ResultType, ilg, services);
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
                EmitMultiply(emitOverflow, unsigned, ilg);
            }
        }

        private static void EmitMultiply(bool emitOverflow, bool unsigned, FleeILGenerator ilg)
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
        /// Emit a string concatenation
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        private void EmitStringConcat(FleeILGenerator ilg, IServiceProvider services)
        {
            Debug.Assert(IsEitherChildOfType(typeof(string)), "one child must be a string");

            // Emit the operands and call the function
            MyLeftChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, typeof(string), ilg, services);
            MyRightChild.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, typeof(string), ilg, services);
            ilg.Emit(OpCodes.Call, _ourStringConcatMethodInfo);
        }

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
