using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Represents a branch from a start location to an end location, along with the label the
    /// branch targets. Used by <see cref="BranchManager"/> to decide whether each branch needs
    /// the long form (5-byte) or short form (2-byte) opcode.
    /// </summary>
    /// <param name="startLocation">The IL position of the branch instruction.</param>
    /// <param name="endLabel">The label the branch targets.</param>
    internal class BranchInfo(ILLocation startLocation, Label endLabel)
    {
        private readonly ILLocation _myStart = startLocation;
        private readonly ILLocation _myEnd = new();
        private readonly Label _myLabel = endLabel;

        /// <summary>
        /// Shifts both endpoints to account for <paramref name="longBranchCount"/> previously
        /// emitted long branches.
        /// </summary>
        /// <param name="longBranchCount">The number of long branches preceding this one.</param>
        public void AdjustForLongBranches(int longBranchCount)
        {
            _myStart.AdjustForLongBranch(longBranchCount);
            // end not necessarily needed once we determine
            // if this is long, but keep it accurate anyway.
            _myEnd.AdjustForLongBranch(longBranchCount);
        }

        /// <summary>
        /// Captures the current long/short decision into <see cref="IsLongBranch"/> so it can
        /// be used during the second IL pass.
        /// </summary>
        public void BakeIsLongBranch()
        {
            IsLongBranch = ComputeIsLongBranch();
        }

        /// <summary>
        /// Shifts only the end location to account for long branches falling between this
        /// branch's start and end.
        /// </summary>
        /// <param name="betweenLongBranchCount">The number of long branches between the endpoints.</param>
        public void AdjustForLongBranchesBetween(int betweenLongBranchCount)
        {
            _myEnd.AdjustForLongBranch(betweenLongBranchCount);
        }

        /// <summary>
        /// Returns whether this branch's start lies between <paramref name="other"/>'s endpoints.
        /// </summary>
        /// <param name="other">The other branch.</param>
        /// <returns><see langword="true"/> when nested.</returns>
        public bool IsBetween(BranchInfo other)
        {
            return _myStart.CompareTo(other._myStart) > 0 && _myStart.CompareTo(other._myEnd) < 0;
        }

        /// <summary>
        /// Recomputes whether the branch needs the long-form opcode given the current endpoints.
        /// </summary>
        /// <returns><see langword="true"/> when long form is required.</returns>
        public bool ComputeIsLongBranch()
        {
            return _myStart.IsLongBranch(_myEnd);
        }

        /// <summary>
        /// If <paramref name="target"/> matches this branch's label, records <paramref name="position"/>
        /// as the end location.
        /// </summary>
        /// <param name="target">The label being marked.</param>
        /// <param name="position">The IL position the label resolves to.</param>
        public void Mark(Label target, int position)
        {
            if (_myLabel.Equals(target))
            {
                _myEnd.SetPosition(position);
            }
        }

        /// <summary>
        /// We only need to compare the start point. There can only be a single branch
        /// emitted at a given start address, so once that matches we can ignore the label.
        /// </summary>
        /// <param name="start">The start location to compare against.</param>
        /// <returns><see langword="true"/> when the starts match.</returns>
        public bool Equals(ILLocation start)
        {
            return _myStart.Equals1(start);
        }

        /// <summary>
        /// Returns a diagnostic string showing the start, end, and long/short classification.
        /// </summary>
        /// <returns>The diagnostic string.</returns>
        public override string ToString()
        {
            return $"{_myStart} -> {_myEnd} (L={_myStart.IsLongBranch(_myEnd)})";
        }

        /// <summary>
        /// Gets the cached long-branch decision, set by <see cref="BakeIsLongBranch"/>.
        /// </summary>
        public bool IsLongBranch { get; private set; }
    }
}
