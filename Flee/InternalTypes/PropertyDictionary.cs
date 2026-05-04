using System.Diagnostics;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Helper class for storing strongly typed properties keyed by case-insensitive name.
    /// Backs the heterogeneous "options bag" used by <see cref="PublicTypes.ExpressionContext"/>
    /// and friends.
    /// </summary>
    internal class PropertyDictionary
    {
        private readonly Dictionary<string, object?> _myProperties;

        /// <summary>
        /// Initializes a new empty dictionary with case-insensitive keys.
        /// </summary>
        public PropertyDictionary()
        {
            _myProperties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a shallow copy of this dictionary. Values are copied by reference.
        /// </summary>
        /// <returns>The copy.</returns>
        public PropertyDictionary Clone()
        {
            PropertyDictionary copy = new();

            foreach (KeyValuePair<string, object?> pair in _myProperties)
            {
                copy.SetValue(pair.Key, pair.Value);
            }

            return copy;
        }

        /// <summary>
        /// Returns the value of the named property cast to <typeparamref name="T"/>.
        /// Asserts in debug builds when the key is missing.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="name">The property name.</param>
        /// <returns>The stored value.</returns>
        public T GetValue<T>(string name)
        {
            if (!_myProperties.TryGetValue(name, out object? value))
            {
                Debug.Fail($"Unknown property '{name}'");
            }
            return (T)value!;
        }

        /// <summary>
        /// Stores <see langword="default"/>(<typeparamref name="T"/>) under <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The value type whose default is stored.</typeparam>
        /// <param name="name">The property name.</param>
        public void SetToDefault<T>(string name)
        {
            T? value = default;
            SetValue(name, value);
        }

        /// <summary>
        /// Stores <paramref name="value"/> under <paramref name="name"/>, replacing any
        /// existing value.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value to store.</param>
        public void SetValue(string name, object? value)
        {
            _myProperties[name] = value;
        }

        /// <summary>
        /// Returns whether a property with the given name has been set.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns><see langword="true"/> when present.</returns>
        public bool Contains(string name)
        {
            return _myProperties.ContainsKey(name);
        }
    }
}
