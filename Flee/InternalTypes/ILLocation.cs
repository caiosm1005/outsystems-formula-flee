namespace Flee.InternalTypes
{
    /// <summary>
    /// Represents a location (byte offset) in an IL stream. Used by <see cref="BranchManager"/>
    /// to track branch start/end positions and decide between short and long branch encodings.
    /// </summary>
    internal class ILLocation : IEquatable<ILLocation>, IComparable<ILLocation>
    {
        private int _myPosition;

        /// <summary>
        /// Long branch is 5 bytes; short branch is 2; so we adjust by the difference.
        /// </summary>
        private const int LongBranchAdjust = 3;

        /// <summary>
        /// Length of the <c>br_s</c> opcode.
        /// </summary>
        private const int BrSLength = 2;

        /// <summary>
        /// Initializes an undefined location at offset 0.
        /// </summary>
        public ILLocation()
        {
        }

        /// <summary>
        /// Initializes a location at the given IL byte offset.
        /// </summary>
        /// <param name="position">The IL byte offset.</param>
        public ILLocation(int position)
        {
            _myPosition = position;
        }

        /// <summary>
        /// Sets this location's IL byte offset.
        /// </summary>
        /// <param name="position">The new offset.</param>
        public void SetPosition(int position)
        {
            _myPosition = position;
        }

        /// <summary>
        /// Shifts this location forward to account for <paramref name="longBranchCount"/> long
        /// branches that appear before it (each adds <see cref="LongBranchAdjust"/> bytes).
        /// </summary>
        /// <param name="longBranchCount">The number of preceding long branches.</param>
        public void AdjustForLongBranch(int longBranchCount)
        {
            _myPosition += longBranchCount * LongBranchAdjust;
        }

        /// <summary>
        /// Returns whether a branch from here to <paramref name="target"/> exceeds the short
        /// branch range and therefore needs the long-form opcode.
        /// </summary>
        /// <param name="target">The branch target location.</param>
        /// <returns><see langword="true"/> when long form is required.</returns>
        public bool IsLongBranch(ILLocation target)
        {
            // The branch offset is relative to the instruction *after* the branch
            // so we add 2 (length of a br_s) to our position
            return Utility.IsLongBranch(_myPosition + BrSLength, target._myPosition);
        }

        /// <summary>
        /// Returns whether two locations refer to the same IL offset.
        /// </summary>
        /// <param name="other">The other location.</param>
        /// <returns><see langword="true"/> when the offsets are equal.</returns>
        public bool Equals1(ILLocation? other)
        {
            return other != null && _myPosition == other._myPosition;
        }

        /// <summary>
        /// Explicit <see cref="IEquatable{T}.Equals(T)"/> implementation.
        /// </summary>
        /// <param name="other">The other location.</param>
        /// <returns><see langword="true"/> when the offsets are equal.</returns>
        bool IEquatable<ILLocation>.Equals(ILLocation? other)
        {
            return Equals1(other);
        }

        /// <summary>
        /// Returns the offset formatted as a hexadecimal string, for diagnostics.
        /// </summary>
        /// <returns>The hex string.</returns>
        public override string ToString()
        {
            return _myPosition.ToString("x");
        }

        /// <summary>
        /// Compares two locations by offset. <see langword="null"/> sorts before anything.
        /// </summary>
        /// <param name="other">The other location.</param>
        /// <returns>A negative/zero/positive integer following the <see cref="IComparable{T}"/> contract.</returns>
        public int CompareTo(ILLocation? other)
        {
            return other == null ? 1 : _myPosition.CompareTo(other._myPosition);
        }
    }
}
