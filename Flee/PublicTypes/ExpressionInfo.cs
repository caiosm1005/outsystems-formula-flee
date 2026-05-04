namespace Flee.PublicTypes
{
    /// <summary>
    /// Metadata collected about a compiled expression, such as the variables it references.
    /// </summary>
    public sealed class ExpressionInfo
    {
        private readonly IDictionary<string, object> _myData;

        /// <summary>
        /// Initializes a new empty <see cref="ExpressionInfo"/> with the standard data slots.
        /// </summary>
        internal ExpressionInfo()
        {
            _myData = new Dictionary<string, object>
            {
                {"ReferencedVariables", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)}
            };
        }

        /// <summary>
        /// Records that the compiled expression references the named variable.
        /// </summary>
        /// <param name="name">The variable name as it appeared in the source text.</param>
        internal void AddReferencedVariable(string name)
        {
            IDictionary<string, string> dict = (IDictionary<string, string>)_myData["ReferencedVariables"];
            dict[name] = name;
        }

        /// <summary>
        /// Returns the names of all variables referenced by the compiled expression.
        /// </summary>
        /// <returns>An array of variable names, in arbitrary order.</returns>
        public string[] GetReferencedVariables()
        {
            IDictionary<string, string> dict = (IDictionary<string, string>)_myData["ReferencedVariables"];
            string[] arr = new string[dict.Count];
            dict.Keys.CopyTo(arr, 0);
            return arr;
        }
    }
}
