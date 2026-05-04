using Flee.InternalTypes;
using Flee.Resources;
using System.ComponentModel;
using System.Reflection;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Holds the variables visible to an <see cref="ExpressionContext"/>. Acts as an
    /// <see cref="IDictionary{TKey, TValue}"/> over <see cref="string"/> → <see cref="object"/>
    /// pairs and exposes events that let callers resolve unknown variables and functions on demand.
    /// </summary>
    public sealed class VariableCollection : IDictionary<string, object>
    {
        private IDictionary<string, IVariable> _myVariables = null!;
        private readonly ExpressionContext _myContext;

        /// <summary>
        /// Raised at compile time when an expression references an unknown variable, so the
        /// handler can declare its CLR type.
        /// </summary>
        public event EventHandler<ResolveVariableTypeEventArgs>? ResolveVariableType;

        /// <summary>
        /// Raised at evaluation time to obtain the value of a variable that wasn't pre-defined.
        /// </summary>
        public event EventHandler<ResolveVariableValueEventArgs>? ResolveVariableValue;

        /// <summary>
        /// Raised at compile time when an expression calls an unknown function, so the handler
        /// can declare its return type.
        /// </summary>
        public event EventHandler<ResolveFunctionEventArgs>? ResolveFunction;

        /// <summary>
        /// Raised at evaluation time to invoke a previously resolved on-demand function.
        /// </summary>
        public event EventHandler<InvokeFunctionEventArgs>? InvokeFunction;

        /// <summary>
        /// Initializes a new collection bound to the given context.
        /// </summary>
        /// <param name="context">The owning expression context.</param>
        internal VariableCollection(ExpressionContext context)
        {
            _myContext = context;
            CreateDictionary();
            HookOptions();
        }

        #region "Methods - Non Public"

        /// <summary>
        /// Subscribes to <see cref="ExpressionOptions.CaseSensitiveChanged"/> so the underlying
        /// dictionary can be rebuilt with a different comparer.
        /// </summary>
        private void HookOptions()
        {
            _myContext.Options.CaseSensitiveChanged += OnOptionsCaseSensitiveChanged;
        }

        /// <summary>
        /// (Re-)creates the internal dictionary with the current case-sensitivity comparer.
        /// </summary>
        private void CreateDictionary()
        {
            _myVariables = new Dictionary<string, IVariable>(_myContext.Options.StringComparer);
        }

        /// <summary>
        /// Handles <see cref="ExpressionOptions.CaseSensitiveChanged"/> by rebuilding the
        /// underlying dictionary.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOptionsCaseSensitiveChanged(object? sender, EventArgs e)
        {
            CreateDictionary();
        }

        /// <summary>
        /// Copies the variables in this collection into <paramref name="dest"/>. Used when
        /// an <see cref="ExpressionContext"/> is cloned.
        /// </summary>
        /// <param name="dest">The destination collection.</param>
        internal void Copy(VariableCollection dest)
        {
            dest.CreateDictionary();
            dest.HookOptions();

            foreach (KeyValuePair<string, IVariable> pair in _myVariables)
            {
                IVariable copyVariable = pair.Value.Clone();
                dest._myVariables.Add(pair.Key, copyVariable);
            }
        }

        /// <summary>
        /// Defines a variable of type <paramref name="variableType"/> with the given initial value.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="variableType">The variable's CLR type.</param>
        /// <param name="variableValue">The initial value (may be <see langword="null"/>).</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="variableType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when a variable with the same name already exists.</exception>
        internal void DefineVariableInternal(string name, Type variableType, object? variableValue)
        {
            Utility.AssertNotNull(variableType, "variableType");

            if (_myVariables.ContainsKey(name))
            {
                string msg = Utility.GetGeneralErrorMessage(
                    GeneralErrorResourceKeys.VariableWithNameAlreadyDefined,
                    name);
                throw new ArgumentException(msg);
            }

            IVariable v = CreateVariable(variableType, variableValue);
            _myVariables.Add(name, v);
        }

        /// <summary>
        /// Returns the CLR type of <paramref name="name"/>, falling back to
        /// <see cref="ResolveVariableType"/> when the variable isn't registered.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>The declared type, or <see langword="null"/> when not resolvable.</returns>
        internal Type? GetVariableTypeInternal(string name)
        {
            bool success = _myVariables.TryGetValue(name, out IVariable? value);

            if (success)
            {
                return value!.VariableType;
            }

            ResolveVariableTypeEventArgs args = new(name);
            ResolveVariableType?.Invoke(this, args);

            return args.VariableType;
        }

        /// <summary>
        /// Looks up a variable by name.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="throwOnNotFound">When <see langword="true"/>, throws if not found.</param>
        /// <returns>The variable, or <see langword="null"/> when not found and not throwing.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when not found and <paramref name="throwOnNotFound"/> is <see langword="true"/>.
        /// </exception>
        private IVariable? GetVariable(string name, bool throwOnNotFound)
        {
            bool success = _myVariables.TryGetValue(name, out IVariable? value);

            if (!success & throwOnNotFound)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.UndefinedVariable, name);
                throw new ArgumentException(msg);
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Constructs the appropriate <see cref="IVariable"/> implementation for the given value.
        /// Wraps <see cref="IExpression"/> values in a generic-expression variable, otherwise
        /// in a plain generic variable.
        /// </summary>
        /// <param name="variableValueType">The CLR type of the value.</param>
        /// <param name="variableValue">The value (may be <see langword="null"/>).</param>
        /// <returns>The created variable.</returns>
        private IVariable CreateVariable(Type variableValueType, object? variableValue)
        {
            Type variableType;

            // Is the variable value an expression?

            if (variableValue is IExpression expression)
            {
                ExpressionOptions options = expression.Context.Options;
                // Get its result type
                variableValueType = options.ResultType;

                // Create a variable that wraps the expression

                variableType = !options.IsGeneric
                    ? typeof(DynamicExpressionVariable<>)
                    : typeof(GenericExpressionVariable<>);
            }
            else
            {
                // Create a variable for a regular value
                _myContext.AssertTypeIsAccessible(variableValueType);
                variableType = typeof(GenericVariable<>);
            }

            // Create the generic variable instance
            variableType = variableType.MakeGenericType(variableValueType);
            IVariable v = (IVariable)Activator.CreateInstance(variableType)!;

            return v;
        }

        /// <summary>
        /// Raises <see cref="ResolveFunction"/> and returns the declared return type, if any.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="argumentTypes">The argument types.</param>
        /// <returns>The declared return type, or <see langword="null"/> when not resolved.</returns>
        internal Type? ResolveOnDemandFunction(string name, Type[] argumentTypes)
        {
            ResolveFunctionEventArgs args = new(name, argumentTypes);
            ResolveFunction?.Invoke(this, args);
            return args.ReturnType;
        }

        /// <summary>
        /// Casts <paramref name="value"/> to <typeparamref name="T"/>, returning <see langword="default"/>
        /// when null.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The value to cast.</param>
        /// <returns>The casted value or <see langword="default"/>.</returns>
        private static T? ReturnGenericValue<T>(object? value)
        {
            return value == null ? default : (T)value;
        }

        /// <summary>
        /// Throws when <paramref name="value"/> cannot be assigned to <paramref name="requiredType"/>.
        /// A <see langword="null"/> value is always allowed.
        /// </summary>
        /// <param name="requiredType">The required type.</param>
        /// <param name="value">The candidate value.</param>
        /// <exception cref="ArgumentException">Thrown when the value is incompatible.</exception>
        private static void ValidateSetValueType(Type requiredType, object? value)
        {
            if (value == null)
            {
                // Can always assign null value
                return;
            }

            Type valueType = value.GetType();

            if (!requiredType.IsAssignableFrom(valueType))
            {
                string msg = Utility.GetGeneralErrorMessage(
                    GeneralErrorResourceKeys.VariableValueNotAssignableToType,
                    valueType.Name,
                    requiredType.Name);
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// Returns the closed-generic <see cref="MethodInfo"/> for
        /// <see cref="GetVariableValueInternal{T}(string)"/> at runtime type <paramref name="variableType"/>.
        /// </summary>
        /// <param name="variableType">The variable's CLR type.</param>
        /// <returns>The closed generic method.</returns>
        internal static MethodInfo GetVariableLoadMethod(Type variableType)
        {
            MethodInfo mi = typeof(VariableCollection).GetMethod(
                "GetVariableValueInternal",
                BindingFlags.Public | BindingFlags.Instance)!;
            mi = mi.MakeGenericMethod(variableType);
            return mi;
        }

        /// <summary>
        /// Returns the closed-generic <see cref="MethodInfo"/> for
        /// <see cref="GetFunctionResultInternal{T}(string, object[])"/> at the given return type.
        /// </summary>
        /// <param name="returnType">The function's return type.</param>
        /// <returns>The closed generic method.</returns>
        internal static MethodInfo GetFunctionInvokeMethod(Type returnType)
        {
            MethodInfo mi = typeof(VariableCollection).GetMethod(
                "GetFunctionResultInternal",
                BindingFlags.Public | BindingFlags.Instance)!;
            mi = mi.MakeGenericMethod(returnType);
            return mi;
        }

        /// <summary>
        /// Returns the closed-generic <see cref="MethodInfo"/> for
        /// <see cref="GetVirtualPropertyValueInternal{T}(string, object)"/> at the given return type.
        /// </summary>
        /// <param name="returnType">The property's return type.</param>
        /// <returns>The closed generic method.</returns>
        internal static MethodInfo GetVirtualPropertyLoadMethod(Type returnType)
        {
            MethodInfo mi = typeof(VariableCollection).GetMethod(
                "GetVirtualPropertyValueInternal",
                BindingFlags.Public | BindingFlags.Instance)!;
            mi = mi.MakeGenericMethod(returnType);
            return mi;
        }

        /// <summary>
        /// Builds a snapshot of variable name → boxed value pairs, for enumeration scenarios.
        /// </summary>
        /// <returns>The snapshot dictionary.</returns>
        private Dictionary<string, object> GetNameValueDictionary()
        {
            Dictionary<string, object> dict = [];

            foreach (KeyValuePair<string, IVariable> pair in _myVariables)
            {
                dict.Add(pair.Key, pair.Value.ValueAsObject);
            }

            return dict;
        }

        #endregion "Methods - Non Public"

        #region "Methods - Public"

        /// <summary>
        /// Returns the declared CLR type of the named variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>The variable's type.</returns>
        /// <exception cref="ArgumentException">Thrown when no such variable exists.</exception>
        public Type GetVariableType(string name)
        {
            IVariable v = GetVariable(name, true)!;
            return v.VariableType;
        }

        /// <summary>
        /// Defines a variable with no initial value (i.e. <see langword="null"/> for reference types,
        /// default for value types).
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="variableType">The variable's CLR type.</param>
        public void DefineVariable(string name, Type variableType)
        {
            DefineVariableInternal(name, variableType, null);
        }

        /// <summary>
        /// Returns the value of a variable as <typeparamref name="T"/>. Falls back to
        /// <see cref="ResolveVariableValue"/> when not pre-defined. This method is invoked from
        /// generated IL — keep its name and signature stable.
        /// </summary>
        /// <typeparam name="T">The variable's CLR type.</typeparam>
        /// <param name="name">The variable name.</param>
        /// <returns>The current value.</returns>
        public T? GetVariableValueInternal<T>(string name)
        {
            if (_myVariables.TryGetValue(name, out IVariable? variable))
            {
                if (variable is IGenericVariable<T> generic)
                {
                    return (T)generic.GetValue()!;
                }
            }

            GenericVariable<T> result;
            GenericVariable<T> vTemp = new();
            ResolveVariableValueEventArgs args = new(name, typeof(T));
            ResolveVariableValue?.Invoke(this, args);

            ValidateSetValueType(typeof(T), args.VariableValue);
            vTemp.ValueAsObject = args.VariableValue!;
            result = vTemp;
            return (T?)result.GetValue();
        }

        /// <summary>
        /// Returns the value of the virtual property <paramref name="name"/> on
        /// <paramref name="component"/>, using <see cref="TypeDescriptor"/>. Invoked from generated IL.
        /// </summary>
        /// <typeparam name="T">The property's CLR type.</typeparam>
        /// <param name="name">The property name.</param>
        /// <param name="component">The owning component.</param>
        /// <returns>The current value.</returns>
        public T? GetVirtualPropertyValueInternal<T>(string name, object component)
        {
            PropertyDescriptorCollection coll = TypeDescriptor.GetProperties(component);
            PropertyDescriptor? pd = coll.Find(name, true);

            object? value = pd?.GetValue(component);
            ValidateSetValueType(typeof(T), value);
            return ReturnGenericValue<T>(value);
        }

        /// <summary>
        /// Invokes an on-demand function via <see cref="InvokeFunction"/> and returns its result
        /// as <typeparamref name="T"/>. Invoked from generated IL.
        /// </summary>
        /// <typeparam name="T">The function's return type.</typeparam>
        /// <param name="name">The function name.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The result.</returns>
        public T? GetFunctionResultInternal<T>(string name, object[] arguments)
        {
            InvokeFunctionEventArgs args = new(name, arguments);
            InvokeFunction?.Invoke(this, args);

            object? result = args.Result;
            ValidateSetValueType(typeof(T), result);

            return ReturnGenericValue<T>(result);
        }

        #endregion "Methods - Public"

        #region "IDictionary Implementation"

        /// <summary>
        /// Internal helper that adds a key/value pair from a single <see cref="KeyValuePair{TKey,TValue}"/>.
        /// </summary>
        /// <param name="item">The pair to add.</param>
        private void Add1(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Explicit <see cref="ICollection{T}.Add"/> implementation for
        /// <see cref="KeyValuePair{TKey, TValue}"/> entries.
        /// </summary>
        /// <param name="item">The pair to add.</param>
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            Add1(item);
        }

        /// <summary>
        /// Removes all variables.
        /// </summary>
        public void Clear()
        {
            _myVariables.Clear();
        }

        /// <summary>
        /// Internal helper that returns whether a key is present in the collection.
        /// </summary>
        /// <param name="item">The pair to look for (only the key is consulted).</param>
        /// <returns><see langword="true"/> when the key exists.</returns>
        private bool Contains1(KeyValuePair<string, object> item)
        {
            return ContainsKey(item.Key);
        }

        /// <summary>
        /// Explicit <see cref="ICollection{T}.Contains"/> implementation for
        /// <see cref="KeyValuePair{TKey, TValue}"/> entries.
        /// </summary>
        /// <param name="item">The pair to look for.</param>
        /// <returns><see langword="true"/> when the key exists.</returns>
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return Contains1(item);
        }

        /// <summary>
        /// Copies snapshot pairs to <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The first index to write to.</param>
        private void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            Dictionary<string, object> dict = GetNameValueDictionary();
            ICollection<KeyValuePair<string, object>> coll = dict;
            coll.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Internal helper that removes the entry whose key equals <paramref name="item"/>'s key.
        /// </summary>
        /// <param name="item">The pair whose key will be removed.</param>
        /// <returns><see langword="true"/> when removed.</returns>
        private bool Remove1(KeyValuePair<string, object> item)
        {
            return Remove(item.Key);
        }

        /// <summary>
        /// Explicit <see cref="ICollection{T}.Remove"/> implementation for
        /// <see cref="KeyValuePair{TKey, TValue}"/> entries.
        /// </summary>
        /// <param name="item">The pair to remove.</param>
        /// <returns><see langword="true"/> when removed.</returns>
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return Remove1(item);
        }

        /// <summary>
        /// Defines a new variable inferring its type from <paramref name="value"/> and assigns
        /// the value.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The initial value (must not be <see langword="null"/>).</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public void Add(string name, object value)
        {
            Utility.AssertNotNull(value, "value");
            DefineVariableInternal(name, value.GetType(), value);
            this[name] = value;
        }

        /// <summary>
        /// Returns whether a variable with the given name has been defined.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns><see langword="true"/> when defined.</returns>
        public bool ContainsKey(string name)
        {
            return _myVariables.ContainsKey(name);
        }

        /// <summary>
        /// Removes the named variable, if present.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns><see langword="true"/> when a variable was removed.</returns>
        public bool Remove(string name)
        {
            return _myVariables.Remove(name);
        }

        /// <summary>
        /// Tries to retrieve the value of a variable.
        /// </summary>
        /// <param name="key">The variable name.</param>
        /// <param name="value">Receives the boxed value when found.</param>
        /// <returns><see langword="true"/> when the variable exists.</returns>
        public bool TryGetValue(string key, out object value)
        {
            IVariable? v = GetVariable(key, false);
            value = v?.ValueAsObject!;
            return v != null!;
        }

        /// <summary>
        /// Returns an enumerator over a snapshot of the variable name/value pairs.
        /// </summary>
        /// <returns>The snapshot enumerator.</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            Dictionary<string, object> dict = GetNameValueDictionary();
            return dict.GetEnumerator();
        }

        /// <summary>
        /// Bridge from the non-generic enumerable contract.
        /// </summary>
        /// <returns>The non-generic enumerator.</returns>
        private System.Collections.IEnumerator GetEnumerator1()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Explicit <see cref="System.Collections.IEnumerable.GetEnumerator"/> implementation.
        /// </summary>
        /// <returns>The non-generic enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }

        /// <summary>
        /// Gets the number of defined variables.
        /// </summary>
        public int Count => _myVariables.Count;

        /// <summary>
        /// Always <see langword="false"/>: the collection accepts mutations.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the value of the variable named <paramref name="name"/>. Setting
        /// auto-defines the variable when it doesn't exist yet, inferring its type from the value.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>The current value.</returns>
        /// <exception cref="ArgumentException">Thrown by the getter when no such variable exists.</exception>
        public object this[string name]
        {
            get
            {
                IVariable v = GetVariable(name, true)!;
                return v.ValueAsObject;
            }
            set
            {
                if (_myVariables.TryGetValue(name, out IVariable? v))
                {
                    v.ValueAsObject = value;
                }
                else
                {
                    Add(name, value);
                }
            }
        }

        /// <summary>
        /// Gets a live view of the variable names.
        /// </summary>
        public ICollection<string> Keys => _myVariables.Keys;

        /// <summary>
        /// Gets a snapshot of the current variable values.
        /// </summary>
        public ICollection<object> Values
        {
            get
            {
                Dictionary<string, object> dict = GetNameValueDictionary();
                return dict.Values;
            }
        }

        /// <summary>
        /// Explicit <see cref="ICollection{T}.CopyTo"/> implementation for
        /// <see cref="KeyValuePair{TKey, TValue}"/> entries.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The first index to write to.</param>
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        #endregion "IDictionary Implementation"
    }
}
