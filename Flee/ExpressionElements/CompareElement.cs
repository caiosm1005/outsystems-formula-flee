using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Literals.Integral;

using Flee.InternalTypes;
using Flee.PublicTypes;


namespace Flee.ExpressionElements
{
    /// <summary>
    /// Comparison operator (<c>=</c>, <c>&lt;&gt;</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c>).
    /// Routes string equality through <see cref="string.Equals(string, string, StringComparison)"/>,
    /// honors user-defined operator overloads, and emits <c>ceq</c>/<c>clt</c>/<c>cgt</c>
    /// patterns for primitives.
    /// </summary>
    internal class CompareElement : BinaryExpressionElement
    {
        private LogicalCompareOperation _myOperation;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public CompareElement()
        {
        }

        /// <summary>
        /// Sets the operands and operator without going through the parser-driven configure path.
        /// Used by elements that build comparison subtrees programmatically.
        /// </summary>
        /// <param name="leftChild">The left operand.</param>
        /// <param name="rightChild">The right operand.</param>
        /// <param name="op">The comparison operator.</param>
        public void Initialize(ExpressionElement leftChild, ExpressionElement rightChild, LogicalCompareOperation op)
        {
            MyLeftChild = leftChild;
            MyRightChild = rightChild;
            _myOperation = op;
        }

        /// <summary>
        /// Runs the standard binary-element validation against the captured operator.
        /// </summary>
        public void Validate()
        {
            ValidateInternal(_myOperation);
        }

        /// <summary>
        /// Stores the parsed <see cref="LogicalCompareOperation"/>.
        /// </summary>
        /// <param name="operation">The operator value from the parser.</param>
        protected override void GetOperation(object operation)
        {
            _myOperation = (LogicalCompareOperation)operation;
        }

        /// <summary>
        /// Resolves the result type, accounting for string equality, overloaded operators,
        /// numeric comparison, boolean equality, reference equality, and same-enum comparison.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <returns>The result type, or <see langword="null"/> when not supported.</returns>
        protected override Type? GetResultType(Type leftType, Type rightType)
        {
            Type? binaryResultType = ImplicitConverter.GetBinaryResultType(leftType, rightType);
            MethodInfo? overloadedOperator = GetOverloadedCompareOperator();
            bool isEqualityOp = IsOpTypeEqualOrNotEqual(_myOperation);

            // Use our string equality instead of overloaded operator
            if (ReferenceEquals(leftType, typeof(string))
                & ReferenceEquals(rightType, typeof(string))
                & isEqualityOp)
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
            else if (ReferenceEquals(leftType, typeof(bool))
                & ReferenceEquals(rightType, typeof(bool))
                & isEqualityOp)
            {
                // Boolean equality
                return typeof(bool);
            }
            else if (AreBothChildrenReferenceTypes() & isEqualityOp)
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
        /// Looks up the overloaded operator method matching <see cref="_myOperation"/>.
        /// </summary>
        /// <returns>The chosen method, or <see langword="null"/> when none is defined.</returns>
        private MethodInfo? GetOverloadedCompareOperator()
        {
            string name = GetCompareOperatorName(_myOperation);
            return GetOverloadedBinaryOperator(name, _myOperation);
        }

        /// <summary>
        /// Maps a <see cref="LogicalCompareOperation"/> to its <c>op_</c> method-name suffix.
        /// </summary>
        /// <param name="op">The operator value.</param>
        /// <returns>The operator function name.</returns>
        private static string GetCompareOperatorName(LogicalCompareOperation op)
        {
            switch (op)
            {
                case LogicalCompareOperation.Equal:
                    return "Equality";
                case LogicalCompareOperation.NotEqual:
                    return "Inequality";
                case LogicalCompareOperation.GreaterThan:
                    return "GreaterThan";
                case LogicalCompareOperation.LessThan:
                    return "LessThan";
                case LogicalCompareOperation.GreaterThanOrEqual:
                    return "GreaterThanOrEqual";
                case LogicalCompareOperation.LessThanOrEqual:
                    return "LessThanOrEqual";
                default:
                    Debug.Assert(false, "unknown compare type");
                    return string.Empty;
            }
        }

        /// <summary>
        /// Emits the comparison: routes through string-equality, overloaded operator, or
        /// primitive/reference compare paths.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type? binaryResultType = ImplicitConverter.GetBinaryResultType(
                MyLeftChild.ResultType,
                MyRightChild.ResultType);
            MethodInfo? overloadedOperator = GetOverloadedCompareOperator();

            if (AreBothChildrenOfType(typeof(string)))
            {
                // String equality
                MyLeftChild.Emit(ilg, services);
                MyRightChild.Emit(ilg, services);
                EmitStringEquality(ilg, _myOperation, services);
            }
            else if (overloadedOperator != null)
            {
                EmitOverloadedOperatorCall(overloadedOperator, ilg, services);
            }
            else if (binaryResultType != null)
            {
                // Emit a compare of numeric operands
                EmitChildWithConvert(MyLeftChild, binaryResultType, ilg, services);
                EmitChildWithConvert(MyRightChild, binaryResultType, ilg, services);
                EmitCompareOperation(ilg, _myOperation);
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
            else if (MyLeftChild.ResultType.IsEnum & MyRightChild.ResultType.IsEnum)
            {
                EmitRegular(ilg, services);
            }
            else
            {
                Debug.Fail("unknown operand types");
            }
        }

        /// <summary>
        /// Emits the standard compare pattern: left, right, compare operation.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitRegular(FleeILGenerator ilg, IServiceProvider services)
        {
            MyLeftChild.Emit(ilg, services);
            MyRightChild.Emit(ilg, services);
            EmitCompareOperation(ilg, _myOperation);
        }

        /// <summary>
        /// Emits a string equality compare via <see cref="string.Equals(string, string, StringComparison)"/>,
        /// inverting the result for <c>!=</c>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="op">The comparison operator.</param>
        /// <param name="services">The compile services (used to read the string comparison option).</param>
        private static void EmitStringEquality(
            FleeILGenerator ilg,
            LogicalCompareOperation op,
            IServiceProvider services)
        {
            // Get the StringComparison from the options
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions))!;
            Int32LiteralElement ic = new((int)options.StringComparison);

            ic.Emit(ilg, services);

            // and emit the method call
            MethodInfo mi = typeof(string).GetMethod(
                "Equals",
                [typeof(string), typeof(string), typeof(StringComparison)],
                null)!;
            ilg.Emit(OpCodes.Call, mi);

            if (op == LogicalCompareOperation.NotEqual)
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Ceq);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="op"/> is the equality or inequality operator.
        /// </summary>
        /// <param name="op">The operator value.</param>
        /// <returns><see langword="true"/> when equality-class.</returns>
        private static bool IsOpTypeEqualOrNotEqual(LogicalCompareOperation op)
        {
            return op == LogicalCompareOperation.Equal | op == LogicalCompareOperation.NotEqual;
        }

        /// <summary>
        /// Returns whether both operands are reference types (used to gate reference equality).
        /// </summary>
        /// <returns><see langword="true"/> when both are reference types.</returns>
        private bool AreBothChildrenReferenceTypes()
        {
            return !MyLeftChild.ResultType.IsValueType & !MyRightChild.ResultType.IsValueType;
        }

        /// <summary>
        /// Returns whether both operands are the same enum type (used to gate enum comparison).
        /// </summary>
        /// <returns><see langword="true"/> when both are the same enum.</returns>
        private bool AreBothChildrenSameEnum()
        {
            return MyLeftChild.ResultType.IsEnum
                && ReferenceEquals(MyLeftChild.ResultType, MyRightChild.ResultType);
        }

        /// <summary>
        /// Emit the actual compare. Translates the logical comparison into the appropriate
        /// IL pattern, sometimes combining <c>ceq</c> with <c>ldc.i4.0</c>+<c>ceq</c> for
        /// "not" semantics.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="op">The comparison operator.</param>
        private void EmitCompareOperation(FleeILGenerator ilg, LogicalCompareOperation op)
        {
            OpCode ltOpcode = GetCompareGTLTOpcode(false);
            OpCode gtOpcode = GetCompareGTLTOpcode(true);

            switch (op)
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
                    Debug.Fail("Unknown op type");
                    break;
            }
        }

        /// <summary>
        /// Get the correct greater/less than opcode. Picks the unsigned variant when both
        /// operands are <see cref="uint"/> or <see cref="ulong"/>.
        /// </summary>
        /// <param name="greaterThan">Whether to return the greater-than opcode.</param>
        /// <returns>The chosen opcode.</returns>
        private OpCode GetCompareGTLTOpcode(bool greaterThan)
        {
            Type leftType = MyLeftChild.ResultType;
            return ReferenceEquals(leftType, MyRightChild.ResultType)
                && (ReferenceEquals(leftType, typeof(UInt32)) | ReferenceEquals(leftType, typeof(UInt64)))
                    ? (greaterThan ? OpCodes.Cgt_Un : OpCodes.Clt_Un)
                    : GetCompareOpcode(greaterThan);
        }

        /// <summary>
        /// Returns the signed compare opcode matching <paramref name="greaterThan"/>.
        /// </summary>
        /// <param name="greaterThan">Whether to return the greater-than opcode.</param>
        /// <returns>The chosen opcode.</returns>
        private static OpCode GetCompareOpcode(bool greaterThan)
        {
            return greaterThan ? OpCodes.Cgt : OpCodes.Clt;
        }
    }
}
