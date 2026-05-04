namespace Flee.CalcEngine.InternalTypes
{
    /// <summary>
    /// Event data raised by the calculation engine when a node finishes recalculating,
    /// carrying the expression name and its new result.
    /// </summary>
    public sealed class NodeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance with default values; populated later via <see cref="SetData"/>.
        /// </summary>
        internal NodeEventArgs()
        {
        }

        /// <summary>
        /// Populates the event with the expression name and its newly computed result.
        /// </summary>
        /// <param name="name">The expression name.</param>
        /// <param name="result">The new result.</param>
        internal void SetData(string name, object result)
        {
            Name = name;
            Result = result;
        }

        /// <summary>
        /// Gets the name of the expression that recalculated.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the most recent result of the expression.
        /// </summary>
        public object Result { get; private set; } = null!;
    }
}
