namespace Flee.InternalTypes
{
    /// <summary>
    /// Identifies a comparison operator emitted by <see cref="ExpressionElements.CompareElement"/>.
    /// </summary>
    internal enum LogicalCompareOperation
    {
        /// <summary>
        /// Less-than (<c>&lt;</c>).
        /// </summary>
        LessThan,

        /// <summary>
        /// Greater-than (<c>&gt;</c>).
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Equality (<c>=</c>).
        /// </summary>
        Equal,

        /// <summary>
        /// Inequality (<c>&lt;&gt;</c>).
        /// </summary>
        NotEqual,

        /// <summary>
        /// Less-than-or-equal (<c>&lt;=</c>).
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Greater-than-or-equal (<c>&gt;=</c>).
        /// </summary>
        GreaterThanOrEqual
    }
}
