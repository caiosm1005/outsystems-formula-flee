namespace Flee.PublicTypes
{
    /// <summary>
    /// Event data raised by <see cref="VariableCollection.ResolveVariableValue"/> so handlers
    /// can supply the value of a variable at evaluation time.
    /// </summary>
    public class ResolveVariableValueEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance for the named variable of the given type.
        /// </summary>
        /// <param name="name">The variable name as it appeared in the expression.</param>
        /// <param name="t">The CLR type the variable was declared as.</param>
        internal ResolveVariableValueEventArgs(string name, Type t)
        {
            VariableName = name;
            VariableType = t;
        }

        /// <summary>
        /// Gets the variable name as it appeared in the expression.
        /// </summary>
        public string VariableName { get; }

        /// <summary>
        /// Gets the CLR type the variable was declared as.
        /// </summary>
        public Type VariableType { get; }

        /// <summary>
        /// Gets or sets the value the handler wants the expression to use for this variable.
        /// </summary>
        public object? VariableValue { get; set; }
    }
}
