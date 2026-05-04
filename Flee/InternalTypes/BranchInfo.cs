using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Represents a branch from a start location to an end location.
    /// </summary>
    internal class BranchInfo(ILLocation startLocation, Label endLabel)
    {
        private readonly ILLocation _myStart = startLocation;
        private readonly ILLocation _myEnd = new();
        private readonly Label _myLabel = endLabel;

        public void AdjustForLongBranches(int longBranchCount)
        {
            _myStart.AdjustForLongBranch(longBranchCount);
            // end not necessarily needed once we determine
            // if this is long, but keep it accurate anyway.
            _myEnd.AdjustForLongBranch(longBranchCount);
        }

        public void BakeIsLongBranch()
        {
            IsLongBranch = ComputeIsLongBranch();
        }

        public void AdjustForLongBranchesBetween(int betweenLongBranchCount)
        {
            _myEnd.AdjustForLongBranch(betweenLongBranchCount);
        }

        public bool IsBetween(BranchInfo other)
        {
            return _myStart.CompareTo(other._myStart) > 0 && _myStart.CompareTo(other._myEnd) < 0;
        }

        public bool ComputeIsLongBranch()
        {
            return _myStart.IsLongBranch(_myEnd);
        }

        public void Mark(Label target, int position)
        {
            if (_myLabel.Equals(target))
            {
                _myEnd.SetPosition(position);
            }
        }

        /// <summary>
        /// We only need to compare the start point. Can only have a single
        /// brach from the exact address, so if label doesn't match we have
        /// bigger problems.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ILLocation start)
        {
            return _myStart.Equals1(start);
        }

        public override string ToString()
        {
            return $"{_myStart} -> {_myEnd} (L={_myStart.IsLongBranch(_myEnd)})";
        }

        public bool IsLongBranch { get; private set; }
    }
}
