namespace Flee.PublicTypes
{
    /// <summary>
    /// Event data raised by <see cref="VariableCollection.InvokeFunction"/> when an on-demand
    /// function call needs to be executed.
    /// </summary>
    public class InvokeFunctionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance for an invocation of <paramref name="name"/> with the
        /// supplied <paramref name="arguments"/>.
        /// </summary>
        /// <param name="name">The function name as it appeared in the expression.</param>
        /// <param name="arguments">The evaluated arguments, in order.</param>
        internal InvokeFunctionEventArgs(string name, object[] arguments)
        {
            FunctionName = name;
            Arguments = arguments;
        }

        /// <summary>
        /// Gets the function name as it appeared in the expression.
        /// </summary>
        public string FunctionName { get; }

        /// <summary>
        /// Gets the evaluated arguments passed to the function, in declaration order.
        /// </summary>
        public object[] Arguments { get; }

        /// <summary>
        /// Gets or sets the value the handler wants the expression to use as the call result.
        /// </summary>
        public object? Result { get; set; }
    }
}
