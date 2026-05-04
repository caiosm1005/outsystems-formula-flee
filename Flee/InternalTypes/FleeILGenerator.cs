using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Wraps a <see cref="ILGenerator"/> with the bookkeeping Flee needs to support its
    /// two-pass emit: tracks IL byte length, caches per-type temporary locals, and routes
    /// branches through a <see cref="BranchManager"/> so short/long opcode selection is correct.
    /// </summary>
    /// <param name="ilg">The underlying <see cref="ILGenerator"/>.</param>
    internal class FleeILGenerator(ILGenerator ilg)
    {
        private ILGenerator _myIlGenerator = ilg;
        private readonly Dictionary<Type, LocalBuilder> _localBuilderTemp = [];
        private int _myPass = 1;
        private readonly BranchManager _bm = new();

        /// <summary>
        /// Returns the local-slot index for a temporary local of the given type, allocating one
        /// on first request and caching it for reuse within the same pass.
        /// </summary>
        /// <param name="localType">The CLR type of the temporary.</param>
        /// <returns>The local slot index.</returns>
        public int GetTempLocalIndex(Type localType)
        {
            if (!_localBuilderTemp.TryGetValue(localType, out LocalBuilder? local))
            {
                local = _myIlGenerator.DeclareLocal(localType);
                _localBuilderTemp.Add(localType, local);
            }

            return local.LocalIndex;
        }

        /// <summary>
        /// After the first pass, checks for long branches. When at least one is required, the
        /// caller emits a second time using <see cref="PrepareSecondPass"/>.
        /// </summary>
        /// <returns><see langword="true"/> when a second pass is needed.</returns>
        public bool NeedsSecondPass()
        {
            return _bm.HasLongBranches();
        }

        /// <summary>
        /// Resets state and switches to a new <see cref="ILGenerator"/> for the second pass.
        /// Also called for a third pass when emitting to a persistable assembly.
        /// </summary>
        /// <param name="ilg">The new IL generator.</param>
        public void PrepareSecondPass(ILGenerator ilg)
        {
            _ = _bm.ComputeBranches();
            _localBuilderTemp.Clear();
            _myIlGenerator = ilg;
            Length = 0;
            _myPass++;
        }

        /// <summary>
        /// Emits an opcode that takes no operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        public void Emit(OpCode op)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="Type"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The type operand.</param>
        public void Emit(OpCode op, Type arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="ConstructorInfo"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The constructor operand.</param>
        public void Emit(OpCode op, ConstructorInfo arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="MethodInfo"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The method operand.</param>
        public void Emit(OpCode op, MethodInfo arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="FieldInfo"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The field operand.</param>
        public void Emit(OpCode op, FieldInfo arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="byte"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, byte arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="sbyte"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, sbyte arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="short"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, short arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with an <see cref="int"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, int arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="long"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, long arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="float"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, float arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="double"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, double arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="string"/> operand.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The operand.</param>
        public void Emit(OpCode op, string arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an opcode with a <see cref="Label"/> operand. Branch opcodes should go through
        /// <see cref="EmitBranch"/>, <see cref="EmitBranchFalse"/>, or <see cref="EmitBranchTrue"/>
        /// instead so they participate in the long/short branch decision.
        /// </summary>
        /// <param name="op">The opcode.</param>
        /// <param name="arg">The label operand.</param>
        public void Emit(OpCode op, Label arg)
        {
            RecordOpcode(op);
            _myIlGenerator.Emit(op, arg);
        }

        /// <summary>
        /// Emits an unconditional branch, choosing between the short and long opcodes based on
        /// the manager's classification.
        /// </summary>
        /// <param name="arg">The branch target.</param>
        public void EmitBranch(Label arg)
        {
            if (_myPass == 1)
            {
                _bm.AddBranch(this, arg);
                Emit(OpCodes.Br_S, arg);
            }
            else if (!_bm.IsLongBranch(this))
            {
                Emit(OpCodes.Br_S, arg);
            }
            else
            {
                Emit(OpCodes.Br, arg);
            }
        }

        /// <summary>
        /// Emits a "branch if false" instruction, choosing between the short and long opcodes.
        /// </summary>
        /// <param name="arg">The branch target.</param>
        public void EmitBranchFalse(Label arg)
        {
            if (_myPass == 1)
            {
                _bm.AddBranch(this, arg);
                Emit(OpCodes.Brfalse_S, arg);
            }
            else if (!_bm.IsLongBranch(this))
            {
                Emit(OpCodes.Brfalse_S, arg);
            }
            else
            {
                Emit(OpCodes.Brfalse, arg);
            }
        }

        /// <summary>
        /// Emits a "branch if true" instruction, choosing between the short and long opcodes.
        /// </summary>
        /// <param name="arg">The branch target.</param>
        public void EmitBranchTrue(Label arg)
        {
            if (_myPass == 1)
            {
                _bm.AddBranch(this, arg);
                Emit(OpCodes.Brtrue_S, arg);
            }
            else if (!_bm.IsLongBranch(this))
            {
                Emit(OpCodes.Brtrue_S, arg);
            }
            else
            {
                Emit(OpCodes.Brtrue, arg);
            }
        }

        /// <summary>
        /// Marks a label at the current IL offset and informs the branch manager.
        /// </summary>
        /// <param name="lbl">The label to mark.</param>
        public void MarkLabel(Label lbl)
        {
            _myIlGenerator.MarkLabel(lbl);
            _bm.MarkLabel(this, lbl);
        }

        /// <summary>
        /// Defines a new label.
        /// </summary>
        /// <returns>The new label.</returns>
        public Label DefineLabel()
        {
            return _myIlGenerator.DefineLabel();
        }

        /// <summary>
        /// Declares a new local of <paramref name="localType"/>.
        /// </summary>
        /// <param name="localType">The local's CLR type.</param>
        /// <returns>The local builder.</returns>
        public LocalBuilder DeclareLocal(Type localType)
        {
            return _myIlGenerator.DeclareLocal(localType);
        }

        /// <summary>
        /// Bumps <see cref="Length"/> by the byte size of the given opcode plus its operand.
        /// </summary>
        /// <param name="op">The opcode being emitted.</param>
        private void RecordOpcode(OpCode op)
        {
            int operandLength = GetOpcodeOperandSize(op.OperandType);
            Length += op.Size + operandLength;
        }

        /// <summary>
        /// Returns the IL operand byte size for <paramref name="operand"/>.
        /// </summary>
        /// <param name="operand">The operand type.</param>
        /// <returns>The operand size in bytes.</returns>
        private static int GetOpcodeOperandSize(OperandType operand)
        {
            if (operand == OperandType.InlineNone)
            {
                return 0;
            }
            if (operand is OperandType.ShortInlineBrTarget
                or OperandType.ShortInlineI
                or OperandType.ShortInlineVar)
            {
                return 1;
            }
            if (operand == OperandType.InlineVar)
            {
                return 2;
            }
            if (operand is OperandType.InlineBrTarget
                or OperandType.InlineField
                or OperandType.InlineI
                or OperandType.InlineMethod
                or OperandType.InlineSig
                or OperandType.InlineString
                or OperandType.InlineTok
                or OperandType.InlineType
                or OperandType.ShortInlineR)
            {
                return 4;
            }
            if (operand is OperandType.InlineI8 or OperandType.InlineR)
            {
                return 8;
            }
            Debug.Fail("Unknown operand type");
            return 0;
        }

        /// <summary>
        /// Asserts that the <see cref="Length"/> we tracked matches the runtime IL offset.
        /// Conditional on DEBUG.
        /// </summary>
        [Conditional("DEBUG")]
        public void ValidateLength()
        {
            Debug.Assert(Length == ILGeneratorLength, "ILGenerator length mismatch");
        }

        /// <summary>
        /// Gets the byte length of IL emitted so far.
        /// </summary>
        public int Length { get; private set; } = 0;

        /// <summary>
        /// Gets the runtime-reported IL offset of the underlying generator.
        /// </summary>
        private int ILGeneratorLength => _myIlGenerator.ILOffset;
    }
}
