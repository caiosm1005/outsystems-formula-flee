namespace Flee.PublicTypes
{
    /// <summary>
    /// Event data raised by <see cref="VariableCollection.ResolveFunction"/> so handlers can
    /// declare the return type of an unknown function at compile time.
    /// </summary>
    public class ResolveFunctionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance describing an unresolved call.
        /// </summary>
        /// <param name="name">The function name as it appeared in the expression.</param>
        /// <param name="argumentTypes">The argument types in the order they were supplied.</param>
        internal ResolveFunctionEventArgs(string name, Type[] argumentTypes)
        {
            FunctionName = name;
            ArgumentTypes = argumentTypes;
        }

        /// <summary>
        /// Gets the function name as it appeared in the expression.
        /// </summary>
        public string FunctionName { get; }

        /// <summary>
        /// Gets the CLR types of the arguments, in declaration order.
        /// </summary>
        public Type[] ArgumentTypes { get; }

        /// <summary>
        /// Gets or sets the return type the handler is declaring for the function. Leave
        /// <see langword="null"/> to indicate the function could not be resolved.
        /// </summary>
        public Type? ReturnType { get; set; }
    }
}
