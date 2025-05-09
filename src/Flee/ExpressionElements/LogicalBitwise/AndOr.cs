﻿using System.Collections;
using System.Diagnostics;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.LogicalBitwise
{
    internal class AndOrElement : BinaryExpressionElement
    {
        private AndOrOperation _myOperation;
        private static readonly object OurTrueTerminalKey = new();
        private static readonly object OurFalseTerminalKey = new();
        private static readonly object OurEndLabelKey = new();

        public static void New()
        {
        }

        protected override void GetOperation(object operation)
        {
            _myOperation = (AndOrOperation)operation;
        }

        protected override Type GetResultType(Type leftType, Type rightType)
        {
            Type bitwiseOpType = Utility.GetBitwiseOpType(leftType, rightType);
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
                ImplicitConverter.EmitImplicitConvert(MyLeftChild.ResultType, resultType, ilg);
                MyRightChild.Emit(ilg, services);
                ImplicitConverter.EmitImplicitConvert(MyRightChild.ResultType, resultType, ilg);
                EmitBitwiseOperation(ilg, _myOperation);
            }
        }

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

        private void DoEmitLogical(FleeILGenerator ilg, IServiceProvider services)
        {
            // We have to do a 'fake' emit so we can get the positions of the labels
            ShortCircuitInfo info = new();

            // Do the real emit
            EmitLogical(ilg, info, services);
        }

        /// <summary>
        /// Emit a short-circuited logical operation sequence
        /// The idea: Store all the leaf operands in a stack with the leftmost at the top and rightmost at the bottom.
        /// For each operand, emit it and try to find an end point for when it short-circuits.  This means we go up through
        /// the stack of operators (ignoring siblings) until we find a different operation (then emit a branch to its right operand)
        /// or we reach the root (emit a branch to a true/false).
        /// Repeat the process for all operands and then emit the true/false/last operand end cases.
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="info"></param>
        /// <param name="services"></param>
        private void EmitLogical(FleeILGenerator ilg, ShortCircuitInfo info, IServiceProvider services)
        {
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
        /// Emit a sequence of and/or expressions with short-circuiting
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="info"></param>
        /// <param name="services"></param>
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
                EmitBranch(op, ilg, l, info);
            }
        }


        private static void EmitBranch(AndOrElement op, FleeILGenerator ilg, Label target, ShortCircuitInfo info)
        {
            // Get the branch opcode
            if (op._myOperation == AndOrOperation.And)
                ilg.EmitBranchFalse(target);
            else
                ilg.EmitBranchTrue(target);
        }


        /// <summary>
        /// Get the label for a short-circuit
        /// </summary>
        /// <param name="current"></param>
        /// <param name="info"></param>
        /// <param name="ilg"></param>
        /// <returns></returns>
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
                if (top._myOperation != current._myOperation)
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
            if (current._myOperation == AndOrOperation.And)
            {
                return GetLabel(OurFalseTerminalKey, ilg, info);
            }
            else
            {
                return GetLabel(OurTrueTerminalKey, ilg, info);
            }
        }

        private void PopRightChild(Stack operands, Stack operators)
        {
            AndOrElement andOrChild = MyRightChild as AndOrElement;

            // What kind of child do we have?
            if (andOrChild != null)
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
        /// Recursively pop operators and operands
        /// </summary>
        /// <param name="operands"></param>
        /// <param name="operators"></param>
        private void Pop(Stack operands, Stack operators)
        {
            operators.Pop();

            AndOrElement andOrChild = MyLeftChild as AndOrElement;
            if (andOrChild == null)
            {
                operands.Pop();
            }
            else
            {
                andOrChild.Pop(operands, operators);
            }

            andOrChild = MyRightChild as AndOrElement;

            if (andOrChild == null)
            {
                operands.Pop();
            }
            else
            {
                andOrChild.Pop(operands, operators);
            }
        }

        private static void EmitOperand(ExpressionElement operand, ShortCircuitInfo info, FleeILGenerator ilg, IServiceProvider services)
        {
            // Is this operand the target of a label?
            if (info.HasLabel(operand) == true)
            {
                // Yes, so mark it
                Label leftLabel = info.FindLabel(operand);
                ilg.MarkLabel(leftLabel);
            }

            // Emit the operand
            operand.Emit(ilg, services);
        }

        /// <summary>
        /// Emit the end cases for a short-circuit
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ilg"></param>
        /// <param name="endLabel"></param>
        private static void EmitTerminals(ShortCircuitInfo info, FleeILGenerator ilg, Label endLabel)
        {
            // Emit the false case if it was used
            if (info.HasLabel(OurFalseTerminalKey) == true)
            {
                Label falseLabel = info.FindLabel(OurFalseTerminalKey);

                // Mark the label and note its position
                ilg.MarkLabel(falseLabel);

                ilg.Emit(OpCodes.Ldc_I4_0);

                // If we also have a true terminal, then skip over it
                if (info.HasLabel(OurTrueTerminalKey) == true)
                {
                    // only 1-3 opcodes, always a short branch
                    ilg.Emit(OpCodes.Br_S, endLabel);
                }
            }

            // Emit the true case if it was used
            if (info.HasLabel(OurTrueTerminalKey) == true)
            {
                Label trueLabel = info.FindLabel(OurTrueTerminalKey);

                // Mark the label and note its position
                ilg.MarkLabel(trueLabel);

                ilg.Emit(OpCodes.Ldc_I4_1);
            }
        }


        private static Label GetLabel(object key, FleeILGenerator ilg, ShortCircuitInfo info)
        {
            if (info.HasLabel(key))
                return info.FindLabel(key);
            return info.AddLabel(key, ilg.DefineLabel());
        }

        /// <summary>
        /// Visit the nodes of the tree (right then left) and populate some data structures
        /// </summary>
        /// <param name="info"></param>
        private void PopulateData(ShortCircuitInfo info)
        {
            // Is our right child a leaf or another And/Or expression?
            AndOrElement andOrChild = MyRightChild as AndOrElement;
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
