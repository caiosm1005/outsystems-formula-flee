namespace Flee.PublicTypes
{
    /// <summary>
    /// Selects which CLR floating-point type a real literal (e.g. <c>1.5</c>) is parsed into.
    /// </summary>
    /// <remarks>
    /// Configured via <see cref="ExpressionOptions.RealLiteralDataType"/>.
    /// </remarks>
    public enum RealLiteralDataType
    {
        /// <summary>
        /// Parse real literals as <see cref="float"/>.
        /// </summary>
        Single,

        /// <summary>
        /// Parse real literals as <see cref="double"/>. This is the default.
        /// </summary>
        Double,

        /// <summary>
        /// Parse real literals as <see cref="decimal"/>.
        /// </summary>
        Decimal
    }
}
