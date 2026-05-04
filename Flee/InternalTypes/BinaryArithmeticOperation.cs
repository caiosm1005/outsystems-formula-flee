namespace Flee.InternalTypes
{
    /// <summary>
    /// Identifies a binary arithmetic operator emitted by <see cref="ExpressionElements.ArithmeticElement"/>.
    /// </summary>
    internal enum BinaryArithmeticOperation
    {
        /// <summary>
        /// Addition (<c>+</c>).
        /// </summary>
        Add,

        /// <summary>
        /// Subtraction (<c>-</c>).
        /// </summary>
        Subtract,

        /// <summary>
        /// Multiplication (<c>*</c>).
        /// </summary>
        Multiply,

        /// <summary>
        /// Division (<c>/</c>).
        /// </summary>
        Divide
    }
}
