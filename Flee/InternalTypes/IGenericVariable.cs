namespace Flee.InternalTypes
{
    /// <summary>
    /// Strongly typed sibling of <see cref="IVariable"/>, exposing the variable's value without boxing.
    /// </summary>
    /// <typeparam name="T">The variable's CLR type.</typeparam>
    internal interface IGenericVariable<T>
    {
        /// <summary>
        /// Returns the current value of the variable.
        /// </summary>
        /// <returns>The value, possibly boxed when reached via this interface.</returns>
        object GetValue();
    }
}
