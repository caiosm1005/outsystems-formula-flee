namespace Flee.InternalTypes
{
    /// <summary>
    /// Identifies a bit-shift operator emitted by <see cref="ExpressionElements.ShiftElement"/>.
    /// </summary>
    internal enum ShiftOperation
    {
        /// <summary>
        /// Left shift (<c>&lt;&lt;</c>).
        /// </summary>
        LeftShift,

        /// <summary>
        /// Right shift (<c>&gt;&gt;</c>).
        /// </summary>
        RightShift
    }
}
