namespace Flee.InternalTypes
{
    /// <summary>
    /// A simple variable holding a value of type <typeparamref name="T"/>. The most common
    /// <see cref="IVariable"/> implementation, used for ordinary CLR values.
    /// </summary>
    /// <typeparam name="T">The variable's CLR type.</typeparam>
    internal class GenericVariable<T> : IVariable, IGenericVariable<T>
    {
        /// <summary>
        /// Holds the variable's value as <see cref="object"/> so reads from generated IL work
        /// uniformly across reference and value types.
        /// </summary>
        public object MyValue = null!;

        /// <summary>
        /// Creates a deep copy of this variable.
        /// </summary>
        /// <returns>A new <see cref="GenericVariable{T}"/> with the same value.</returns>
        public IVariable Clone()
        {
            GenericVariable<T> copy = new() { MyValue = MyValue };
            return copy;
        }

        /// <summary>
        /// Returns the current value (boxed when <typeparamref name="T"/> is a value type).
        /// </summary>
        /// <returns>The current value.</returns>
        public object GetValue()
        {
            return MyValue;
        }

        /// <summary>
        /// Gets the variable's CLR type.
        /// </summary>
        public Type VariableType => typeof(T);

        /// <summary>
        /// Gets or sets the boxed value. A <see langword="null"/> assignment on a value-typed
        /// variable falls back to <see langword="default"/>(<typeparamref name="T"/>).
        /// </summary>
        public object ValueAsObject
        {
            get => MyValue;
            set => MyValue = value ?? default(T)!;
        }
    }
}
