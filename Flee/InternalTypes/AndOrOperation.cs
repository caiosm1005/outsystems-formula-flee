namespace Flee.InternalTypes
{
    /// <summary>
    /// Identifies a short-circuit logical operator emitted by
    /// <see cref="ExpressionElements.LogicalBitwise.AndOrElement"/>.
    /// </summary>
    internal enum AndOrOperation
    {
        /// <summary>
        /// Logical AND (<c>and</c>, <c>&amp;&amp;</c>).
        /// </summary>
        And,

        /// <summary>
        /// Logical OR (<c>or</c>, <c>||</c>).
        /// </summary>
        Or
    }
}
