namespace Flee.PublicTypes
{
    /// <summary>
    /// Event data raised by <see cref="VariableCollection.ResolveVariableType"/> so handlers
    /// can declare the type of an unknown variable at compile time.
    /// </summary>
    public class ResolveVariableTypeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance for the named variable.
        /// </summary>
        /// <param name="name">The variable name as it appeared in the expression.</param>
        internal ResolveVariableTypeEventArgs(string name)
        {
            VariableName = name;
        }

        /// <summary>
        /// Gets the variable name as it appeared in the expression.
        /// </summary>
        public string VariableName { get; }

        /// <summary>
        /// Gets or sets the type the handler is declaring for the variable. Leave
        /// <see langword="null"/> to indicate the variable could not be resolved.
        /// </summary>
        public Type? VariableType { get; set; }
    }
}
