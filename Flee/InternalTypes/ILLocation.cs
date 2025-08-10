namespace Flee.InternalTypes
{
    /// <summary>
    /// Represents a location in an IL stream.
    /// </summary>
    internal class ILLocation : IEquatable<ILLocation>, IComparable<ILLocation>
    {
        private int _myPosition;

        /// <summary>
        /// ' Long branch is 5 bytes; short branch is 2; so we adjust by the difference
        /// </summary>
        private const int LongBranchAdjust = 3;

        /// <summary>
        /// Length of the Br_s opcode
        /// </summary>
        private const int BrSLength = 2;

        public ILLocation()
        {
        }

        public ILLocation(int position)
        {
            _myPosition = position;
        }

        public void SetPosition(int position)
        {
            _myPosition = position;
        }

        /// <summary>
        /// Adjust our position by a certain amount of long branches
        /// </summary>
        /// <param name="longBranchCount"></param>
        /// <remarks></remarks>
        public void AdjustForLongBranch(int longBranchCount)
        {
            _myPosition += longBranchCount * LongBranchAdjust;
        }

        /// <summary>
        /// Determine if this branch is long
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public bool IsLongBranch(ILLocation target)
        {
            // The branch offset is relative to the instruction *after* the branch so we add 2 (length of a br_s) to our position
            return Utility.IsLongBranch(_myPosition + BrSLength, target._myPosition);
        }

        public bool Equals1(ILLocation other)
        {
            return _myPosition == other._myPosition;
        }
        bool System.IEquatable<ILLocation>.Equals(ILLocation other)
        {
            return Equals1(other);
        }

        public override string ToString()
        {
            return _myPosition.ToString("x");
        }

        public int CompareTo(ILLocation other)
        {
            return _myPosition.CompareTo(other._myPosition);
        }
    }
}
