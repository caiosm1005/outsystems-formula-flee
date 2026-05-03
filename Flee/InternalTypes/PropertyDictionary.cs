using System.Diagnostics;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Helper class for storing strongly-typed properties.
    /// </summary>
    internal class PropertyDictionary
    {
        private readonly Dictionary<string, object?> _myProperties;
        public PropertyDictionary()
        {
            _myProperties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        public PropertyDictionary Clone()
        {
            PropertyDictionary copy = new PropertyDictionary();

            foreach (KeyValuePair<string, object?> pair in _myProperties)
            {
                copy.SetValue(pair.Key, pair.Value);
            }

            return copy;
        }

        public T GetValue<T>(string name)
        {
            object? value = default(T);
            if (_myProperties.TryGetValue(name, out value) == false)
            {
                Debug.Fail($"Unknown property '{name}'");
            }
            return (T)value!;
        }

        public void SetToDefault<T>(string name)
        {
            T? value = default(T);
            this.SetValue(name, value);
        }

        public void SetValue(string name, object? value)
        {
            _myProperties[name] = value;
        }

        public bool Contains(string name)
        {
            return _myProperties.ContainsKey(name);
        }
    }
}
