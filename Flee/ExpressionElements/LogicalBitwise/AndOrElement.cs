using System.Collections;
using System.Diagnostics;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.LogicalBitwise
{
    /// <summary>
    /// Short-circuited <c>and</c>/<c>or</c> for booleans, plus bitwise <c>&amp;</c>/<c>|</c>
    /// for integrals. The boolean path emits a tree-walking IL pattern that branches to a
    /// shared true/false terminal as soon as the result is determined.
    /// </summary>
    internal class AndOrElement : BinaryExpressionElement
    {
        private AndOrOperation _myOperation;
        private static readonly object OurTrueTerminalKey = new();
        private static readonly object OurFalseTerminalKey = new();

        /// <summary>
        /// Legacy parameterless initialization helper retained for compatibility with the
        /// VB.NET origin of the codebase. Currently a no-op.
        /// </summary>
        public void New()
        {
        }

        /// <summary>
        /// Stores the parsed <see cref="AndOrOperation"/>.
        /// </summary>
        /// <param name="operation">The operator value from the parser.</param>
        protected override void GetOperation(object operation)
        {
            _myOperation = (AndOrOperation)operation;
        }

        /// <summary>
        /// Returns the bitwise result type when both operands are integral, <see cref="bool"/>
        /// when both operands are boolean, otherwise <see langword="null"/>.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <returns>The result type, or <see langword="null"/>.</returns>
        protected override Type? GetResultType(Type leftType, Type rightType)
        {
            Type? bitwiseOpType = Utility.GetBitwiseOpType(leftType, rightType);
            return bitwiseOpType ?? (AreBothChildrenOfType(typeof(bool)) ? typeof(bool) : null);
        }

        /// <summary>
        /// Emits the boolean short-circuit form when the result type is <see cref="bool"/>;
        /// otherwise emits a straightforward bitwise <c>and</c>/<c>or</c>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type resultType = ResultType;

            if (ReferenceEquals(resultType, typeof(bool)))
            {
                DoEmitLogical(ilg, services);
            }
            else
            {
                MyLeftChild.Emit(ilg, services);
                _ = ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, resultType, ilg);
                MyRightChild.Emit(ilg, services);
                _ = ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, resultType, ilg);
                EmitBitwiseOperation(ilg, _myOperation);
            }
        }

        /// <summary>
        /// Emits the appropriate primitive bitwise opcode.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="op">The operator value.</param>
        private static void EmitBitwiseOperation(FleeILGenerator ilg, AndOrOperation op)
        {
            switch (op)
            {
                case AndOrOperation.And:
                    ilg.Emit(OpCodes.And);
                    break;
                case AndOrOperation.Or:
                    ilg.Emit(OpCodes.Or);
                    break;
                default:
                    Debug.Fail("Unknown op type");
                    break;
            }
        }

        /// <summary>
        /// Sets up the per-emit short-circuit tracking and dispatches to the tree-walking
        /// emit.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void DoEmitLogical(FleeILGenerator ilg, IServiceProvider services)
        {
            // We have to do a 'fake' emit so we can get the positions of the labels
            ShortCircuitInfo info = new();

            // Do the real emit
            EmitLogical(ilg, info, services);
        }

        /// <summary>
        /// Emit a short-circuited logical operation sequence. The idea: store all the leaf
        /// operands in a stack with the leftmost at the top and rightmost at the bottom.
        /// For each operand, emit it and try to find an end point for when it short-circuits.
        /// This means we go up through the stack of operators (ignoring siblings) until we
        /// find a different operation (then emit a branch to its right operand) or we reach
        /// the root (emit a branch to a true/false). Repeat the process for all operands and
        /// then emit the true/false/last operand end cases.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="info">The per-emit short-circuit state.</param>
        /// <param name="services">The compile services.</param>
        private void EmitLogical(FleeILGenerator ilg, ShortCircuitInfo info, IServiceProvider services)
        {
            // We always have an end label
            Label endLabel = ilg.DefineLabel();

            // Populate our data structures
            PopulateData(info);

            // Emit the sequence
            EmitLogicalShortCircuit(ilg, info, services);

            // Get the last operand
            ExpressionElement terminalOperand = (ExpressionElement)info.Operands.Pop()!;
            // Emit it
            EmitOperand(terminalOperand, info, ilg, services);

            // only 1-3 opcodes, always a short branch
            ilg.EmitBranch(endLabel);

            // Emit our true/false terminals
            EmitTerminals(info, ilg, endLabel);

            // Mark the end
            ilg.MarkLabel(endLabel);
        }

        /// <summary>
        /// Emit a sequence of and/or expressions with short-circuiting. Each loop iteration
        /// pops one operator and its left operand, emits the operand, then branches to the
        /// next short-circuit label.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="info">The per-emit short-circuit state.</param>
        /// <param name="services">The compile services.</param>
        private static void EmitLogicalShortCircuit(
            FleeILGenerator ilg,
            ShortCircuitInfo info,
            IServiceProvider services)
        {
            while (info.Operators.Count != 0)
            {
                // Get the operator
                AndOrElement op = (AndOrElement)info.Operators.Pop()!;
                // Get the left operand
                ExpressionElement leftOperand = (ExpressionElement)info.Operands.Pop()!;

                // Emit the left
                EmitOperand(leftOperand, info, ilg, services);

                // Get the label for the short-circuit case
                Label l = GetShortCircuitLabel(op, info, ilg);
                // Emit the branch
                EmitBranch(op, ilg, l);
            }
        }

        /// <summary>
        /// Emits the conditional branch to <paramref name="target"/>: <c>brfalse</c> for
        /// AND-style short-circuit, <c>brtrue</c> for OR-style.
        /// </summary>
        /// <param name="op">The operator element being emitted.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="target">The branch target.</param>
        private static void EmitBranch(AndOrElement op, FleeILGenerator ilg, Label target)
        {
            // Get the branch opcode
            if (op._myOperation == AndOrOperation.And)
            {
                ilg.EmitBranchFalse(target);
            }
            else
            {
                ilg.EmitBranchTrue(target);
            }
        }

        /// <summary>
        /// Get the label for a short-circuit. Walks up the operator stack looking for a
        /// different operation; when found, returns the label of that operation's right
        /// operand. When the walk exhausts, returns the appropriate true/false terminal label.
        /// </summary>
        /// <param name="current">The operator currently being emitted.</param>
        /// <param name="info">The per-emit short-circuit state.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <returns>The branch target label.</returns>
        private static Label GetShortCircuitLabel(
            AndOrElement current,
            ShortCircuitInfo info,
            FleeILGenerator ilg)
        {
            // We modify the given stacks so we need to clone them
            Stack cloneOperands = (Stack)info.Operands.Clone();
            Stack cloneOperators = (Stack)info.Operators.Clone();

            // Pop all siblings
            current.PopRightChild(cloneOperands, cloneOperators);

            // Go until we run out of operators
            while (cloneOperators.Count > 0)
            {
                // Get the top operator
                AndOrElement top = (AndOrElement)cloneOperators.Pop()!;

                // Is is a different operation?
                if (top._myOperation != current._myOperation)
                {
                    // Yes, so return a label to its right operand
                    object nextOperand = cloneOperands.Pop()!;
                    return GetLabel(nextOperand, ilg, info);
                }
                else
                {
                    // No, so keep going up the stack
                    top.PopRightChild(cloneOperands, cloneOperators);
                }
            }

            // We've reached the end of the stack so return the label for the appropriate true/false terminal
            return current._myOperation == AndOrOperation.And
                ? GetLabel(OurFalseTerminalKey, ilg, info)
                : GetLabel(OurTrueTerminalKey, ilg, info);
        }

        /// <summary>
        /// Pops the right child off <paramref name="operands"/> when it's a leaf, or recursively
        /// when it's a nested and/or expression.
        /// </summary>
        /// <param name="operands">The operand stack to mutate.</param>
        /// <param name="operators">The operator stack to mutate.</param>
        private void PopRightChild(Stack operands, Stack operators)
        {
            // What kind of child do we have?
            if (MyRightChild is AndOrElement andOrChild)
            {
                // Another and/or expression so recurse
                andOrChild.Pop(operands, operators);
            }
            else
            {
                // A terminal so pop it off the operands stack
                _ = operands.Pop();
            }
        }

        /// <summary>
        /// Recursively pop operators and operands corresponding to this subtree.
        /// </summary>
        /// <param name="operands">The operand stack to mutate.</param>
        /// <param name="operators">The operator stack to mutate.</param>
        private void Pop(Stack operands, Stack operators)
        {
            _ = operators.Pop();

            AndOrElement? andOrChild = MyLeftChild as AndOrElement;
            if (andOrChild == null)
            {
                _ = operands.Pop();
            }
            else
            {
                andOrChild.Pop(operands, operators);
            }

            andOrChild = MyRightChild as AndOrElement;

            if (andOrChild == null)
            {
                _ = operands.Pop();
            }
            else
            {
                andOrChild.Pop(operands, operators);
            }
        }

        /// <summary>
        /// Emits an operand, marking the cached label first when one was registered for it.
        /// </summary>
        /// <param name="operand">The operand to emit.</param>
        /// <param name="info">The per-emit short-circuit state.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private static void EmitOperand(
            ExpressionElement operand,
            ShortCircuitInfo info,
            FleeILGenerator ilg,
            IServiceProvider services)
        {
            // Is this operand the target of a label?
            if (info.HasLabel(operand))
            {
                // Yes, so mark it
                Label leftLabel = info.FindLabel(operand);
                ilg.MarkLabel(leftLabel);
            }

            // Emit the operand
            operand.Emit(ilg, services);
        }

        /// <summary>
        /// Emit the end cases for a short-circuit. The false terminal pushes <c>0</c>; the
        /// true terminal pushes <c>1</c>; when both are needed, a branch to the end label
        /// separates them.
        /// </summary>
        /// <param name="info">The per-emit short-circuit state.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="endLabel">The end label for the boolean expression.</param>
        private static void EmitTerminals(ShortCircuitInfo info, FleeILGenerator ilg, Label endLabel)
        {
            // Emit the false case if it was used
            if (info.HasLabel(OurFalseTerminalKey))
            {
                Label falseLabel = info.FindLabel(OurFalseTerminalKey);

                // Mark the label and note its position
                ilg.MarkLabel(falseLabel);

                ilg.Emit(OpCodes.Ldc_I4_0);

                // If we also have a true terminal, then skip over it
                if (info.HasLabel(OurTrueTerminalKey))
                {
                    // only 1-3 opcodes, always a short branch
                    ilg.Emit(OpCodes.Br_S, endLabel);
                }
            }

            // Emit the true case if it was used
            if (info.HasLabel(OurTrueTerminalKey))
            {
                Label trueLabel = info.FindLabel(OurTrueTerminalKey);

                // Mark the label and note its position
                ilg.MarkLabel(trueLabel);

                ilg.Emit(OpCodes.Ldc_I4_1);
            }
        }

        /// <summary>
        /// Returns the cached label for <paramref name="key"/>, allocating one on first use.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="info">The per-emit short-circuit state.</param>
        /// <returns>The label.</returns>
        private static Label GetLabel(object key, FleeILGenerator ilg, ShortCircuitInfo info)
        {
            return info.HasLabel(key)
                ? info.FindLabel(key)
                : info.AddLabel(key, ilg.DefineLabel());
        }

        /// <summary>
        /// Visit the nodes of the tree (right then left) and populate the operand/operator
        /// stacks used by <see cref="EmitLogicalShortCircuit"/>.
        /// </summary>
        /// <param name="info">The per-emit short-circuit state.</param>
        private void PopulateData(ShortCircuitInfo info)
        {
            // Is our right child a leaf or another And/Or expression?
            AndOrElement? andOrChild = MyRightChild as AndOrElement;
            if (andOrChild == null)
            {
                // Leaf so push it on the stack
                info.Operands.Push(MyRightChild);
            }
            else
            {
                // Another And/Or expression so recurse
                andOrChild.PopulateData(info);
            }

            // Add ourselves as an operator
            info.Operators.Push(this);

            // Do the same thing for the left child
            andOrChild = MyLeftChild as AndOrElement;

            if (andOrChild == null)
            {
                info.Operands.Push(MyLeftChild);
            }
            else
            {
                andOrChild.PopulateData(info);
            }
        }
    }
}
