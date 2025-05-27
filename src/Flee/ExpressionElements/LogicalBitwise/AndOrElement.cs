using System.Collections;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.LogicalBitwise
{
    /// <summary>
    /// Specifies the types of logical AND/OR operations supported by the Flee expression engine. Used to represent
    /// logical and bitwise AND/OR operators in parsed expressions.
    /// </summary>
    internal enum AndOrOperation
    {
        And,
        Or
    }

    /// <summary>
    /// Represents a logical or bitwise AND/OR operation within the Flee expression engine. Supports both
    /// short-circuiting logical operations for boolean operands and bitwise operations for integral types.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AndOrElement"/> class with the specified operands and operation.
    /// </remarks>
    /// <param name="leftChild">The left operand.</param>
    /// <param name="rightChild">The right operand.</param>
    /// <param name="operation">The AND/OR operation to perform.</param>
    internal class AndOrElement(ExpressionElement leftChild, ExpressionElement rightChild, AndOrOperation operation) :
        BinaryExpressionElement(leftChild, rightChild, operation)
    {
        private static readonly object OurTrueTerminalKey = new();
        private static readonly object OurFalseTerminalKey = new();

        /// <summary>
        /// Gets the label for a short-circuit in a logical operation.
        /// </summary>
        /// <param name="current">The current <see cref="AndOrElement"/> node.</param>
        /// <param name="info">Short-circuit info structure.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <returns>The label to branch to for short-circuiting.</returns>
        private static Label GetShortCircuitLabel(AndOrElement current, ShortCircuitInfo info, FleeILGenerator ilg)
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
                AndOrElement top = (AndOrElement)cloneOperators.Pop();

                // Is is a different operation?
                if (top._operation != current._operation)
                {
                    // Yes, so return a label to its right operand
                    object nextOperand = cloneOperands.Pop();
                    return GetLabel(nextOperand, ilg, info);
                }
                else
                {
                    // No, so keep going up the stack
                    top.PopRightChild(cloneOperands, cloneOperators);
                }
            }

            // We've reached the end of the stack so return the label for the appropriate true/false terminal
            if ((AndOrOperation)current._operation == AndOrOperation.And)
            {
                return GetLabel(OurFalseTerminalKey, ilg, info);
            }
            else
            {
                return GetLabel(OurTrueTerminalKey, ilg, info);
            }
        }

        /// <summary>
        /// Gets or creates a label for the specified key in the short-circuit info.
        /// </summary>
        /// <param name="key">The key for the label.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="info">Short-circuit info structure.</param>
        /// <returns>The label associated with the key.</returns>
        private static Label GetLabel(object key, FleeILGenerator ilg, ShortCircuitInfo info)
        {
            if (info.HasLabel(key))
            {
                return info.FindLabel(key);
            }
            return info.AddLabel(key, ilg.DefineLabel());
        }

        /// <summary>
        /// Emits the IL for a bitwise AND or OR operation.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="operation">The AND/OR operation to emit.</param>
        private static void EmitBitwiseOperation(FleeILGenerator ilg, AndOrOperation operation)
        {
            switch (operation)
            {
                case AndOrOperation.And:
                    ilg.Emit(OpCodes.And);
                    break;

                case AndOrOperation.Or:
                    ilg.Emit(OpCodes.Or);
                    break;

                default:
                    throw new NotImplementedException($"Operation '{Enum.GetName(operation.GetType(), operation)}' not implemented.");
            }
        }

        /// <summary>
        /// Emits a sequence of AND/OR expressions with short-circuiting logic for boolean operands.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="info">Short-circuit info structure.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private static void EmitLogicalShortCircuit(FleeILGenerator ilg, ShortCircuitInfo info, IServiceProvider services)
        {
            while (info.Operators.Count != 0)
            {
                // Get the operator
                AndOrElement op = (AndOrElement)info.Operators.Pop();
                // Get the left operand
                ExpressionElement leftOperand = (ExpressionElement)info.Operands.Pop();

                // Emit the left
                EmitOperand(leftOperand, info, ilg, services);

                // Get the label for the short-circuit case
                Label l = GetShortCircuitLabel(op, info, ilg);
                // Emit the branch
                EmitBranch(op, ilg, l);
            }
        }

        /// <summary>
        /// Emits a branch instruction for the specified logical operation.
        /// </summary>
        /// <param name="element">The AND/OR element.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="target">The label to branch to.</param>
        private static void EmitBranch(AndOrElement element, FleeILGenerator ilg, Label target)
        {
            // Get the branch opcode
            if ((AndOrOperation)element._operation == AndOrOperation.And)
                ilg.EmitBranchFalse(target);
            else
                ilg.EmitBranchTrue(target);
        }

        /// <summary>
        /// Emits the IL for an operand, marking its label if necessary.
        /// </summary>
        /// <param name="operand">The operand to emit.</param>
        /// <param name="info">Short-circuit info structure.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private static void EmitOperand(ExpressionElement operand, ShortCircuitInfo info, FleeILGenerator ilg,
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
        /// Emits the end cases for a short-circuit logical operation.
        /// </summary>
        /// <param name="info">Short-circuit info structure.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="endLabel">The end label for the operation.</param>
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
        /// Pops the right child from the operand and operator stacks, recursing if the child is another
        /// <see cref="AndOrElement"/>.
        /// </summary>
        /// <param name="operands">The operand stack.</param>
        /// <param name="operators">The operator stack.</param>
        private void PopRightChild(Stack operands, Stack operators)
        {
            // What kind of child do we have?
            if (_rightChild is AndOrElement andOrChild)
            {
                // Another and/or expression so recurse
                andOrChild.Pop(operands, operators);
            }
            else
            {
                // A terminal so pop it off the operands stack
                operands.Pop();
            }
        }

        /// <summary>
        /// Recursively pops operators and operands for tree traversal.
        /// </summary>
        /// <param name="operands">The operand stack.</param>
        /// <param name="operators">The operator stack.</param>
        private void Pop(Stack operands, Stack operators)
        {
            operators.Pop();

            if (_leftChild is not AndOrElement andOrChildLeft)
            {
                operands.Pop();
            }
            else
            {
                andOrChildLeft.Pop(operands, operators);
            }

            if (_rightChild is not AndOrElement andOrChildRight)
            {
                operands.Pop();
            }
            else
            {
                andOrChildRight.Pop(operands, operators);
            }
        }

        /// <summary>
        /// Visits the nodes of the tree (right then left) and populates the short-circuit info data structures.
        /// </summary>
        /// <param name="info">Short-circuit info structure.</param>
        private void PopulateData(ShortCircuitInfo info)
        {
            // Is our right child a leaf or another And/Or expression?
            if (_rightChild is not AndOrElement andOrChildRight)
            {
                // Leaf so push it on the stack
                info.Operands.Push(_rightChild);
            }
            else
            {
                // Another And/Or expression so recurse
                andOrChildRight.PopulateData(info);
            }

            // Add ourselves as an operator
            info.Operators.Push(this);

            // Do the same thing for the left child
            if (_leftChild is not AndOrElement andOrChildLeft)
            {
                info.Operands.Push(_leftChild);
            }
            else
            {
                andOrChildLeft.PopulateData(info);
            }
        }

        /// <summary>
        /// Emits the IL for a logical operation with short-circuiting.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private void DoEmitLogical(FleeILGenerator ilg, IServiceProvider services)
        {
            // We have to do a 'fake' emit so we can get the positions of the labels
            ShortCircuitInfo info = new();

            // Do the real emit
            EmitLogical(ilg, info, services);
        }

        /// <summary>
        /// Emits a short-circuited logical operation sequence.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="info">Short-circuit info structure.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        private void EmitLogical(FleeILGenerator ilg, ShortCircuitInfo info, IServiceProvider services)
        {
            // The idea: Store all the leaf operands in a stack with the leftmost at the top and rightmost at the
            // bottom. For each operand, emit it and try to find an end point for when it short-circuits. This means we
            // go up through the stack of operators (ignoring siblings) until we find a different operation (then emit a
            // branch to its right operand) or we reach the root (emit a branch to a true/false). Repeat the process for
            // all operands and then emit the true/false/last operand end cases.

            // We always have an end label
            Label endLabel = ilg.DefineLabel();

            // Populate our data structures
            PopulateData(info);

            // Emit the sequence
            EmitLogicalShortCircuit(ilg, info, services);

            // Get the last operand
            ExpressionElement terminalOperand = (ExpressionElement)info.Operands.Pop();
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
        /// Resolves the result type for the AND/OR operation, supporting both bitwise and logical operations.
        /// </summary>
        /// <param name="leftType">The type of the left operand.</param>
        /// <param name="rightType">The type of the right operand.</param>
        /// <returns>The result type, or null if the operation is not defined for the given types.</returns>
        protected override Type? ResolveResultType(Type leftType, Type rightType)
        {
            Type? bitwiseOpType = Utility.GetBitwiseOpType(leftType, rightType);
            if (bitwiseOpType != null)
            {
                return bitwiseOpType;
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
        /// Emits the IL code for this AND/OR expression element, handling both logical and bitwise operations.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">A service provider for dependency resolution.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Type resultType = ResultType;

            if (ReferenceEquals(resultType, typeof(bool)))
            {
                DoEmitLogical(ilg, services);
            }
            else
            {
                _leftChild.Emit(ilg, services);
                ImplicitConverter.EmitImplicitConvert(_leftChild.ResultType, resultType, ilg);
                _rightChild.Emit(ilg, services);
                ImplicitConverter.EmitImplicitConvert(_rightChild.ResultType, resultType, ilg);
                EmitBitwiseOperation(ilg, (AndOrOperation)_operation);
            }
        }
    }
}
