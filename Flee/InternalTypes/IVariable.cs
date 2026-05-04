namespace Flee.InternalTypes
{
    /// <summary>
    /// Common contract for variables tracked by <see cref="PublicTypes.VariableCollection"/> —
    /// supports cloning, type queries, and boxed value access.
    /// </summary>
    internal interface IVariable
    {
        /// <summary>
        /// Creates a deep copy of this variable.
        /// </summary>
        /// <returns>A new <see cref="IVariable"/> with the same state.</returns>
        IVariable Clone();

        /// <summary>
        /// Gets the CLR type of the variable's value.
        /// </summary>
        Type VariableType { get; }

        /// <summary>
        /// Gets or sets the boxed value of the variable.
        /// </summary>
        object ValueAsObject { get; set; }
    }
}
