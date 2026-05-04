using System.Globalization;
using System.Reflection;

namespace Flee.InternalTypes
{
    /// <summary>
    /// A reflection <see cref="Binder"/> that picks the explicit-conversion operator overload
    /// matching exactly <paramref name="returnType"/> and <paramref name="argType"/>.
    /// </summary>
    /// <param name="returnType">The desired return type of the operator.</param>
    /// <param name="argType">The desired argument type of the operator.</param>
    internal class ExplicitOperatorMethodBinder(Type returnType, Type argType) : CustomBinder
    {
        private readonly Type _myReturnType = returnType;
        private readonly Type _myArgType = argType;

        /// <summary>
        /// Not used; overload resolution happens in <see cref="SelectMethod"/>.
        /// </summary>
        /// <param name="bindingAttr">Ignored.</param>
        /// <param name="match">Ignored.</param>
        /// <param name="args">Ignored.</param>
        /// <param name="modifiers">Ignored.</param>
        /// <param name="culture">Ignored.</param>
        /// <param name="names">Ignored.</param>
        /// <param name="state">Set to <see langword="null"/>.</param>
        /// <returns>Always <see langword="null"/>.</returns>
        public override MethodBase BindToMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            ref object?[] args,
            ParameterModifier[]? modifiers,
            CultureInfo? culture,
            string[]? names,
            out object? state)
        {
            state = null;
            return null!;
        }

        /// <summary>
        /// Returns the first method in <paramref name="match"/> whose first parameter type
        /// and return type are reference-equal to the configured operand and result types.
        /// </summary>
        /// <param name="bindingAttr">Ignored.</param>
        /// <param name="match">The candidate methods.</param>
        /// <param name="types">Ignored.</param>
        /// <param name="modifiers">Ignored.</param>
        /// <returns>The matching method, or <see langword="null"/> when none qualifies.</returns>
        public override MethodBase? SelectMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            Type[] types,
            ParameterModifier[]? modifiers)
        {
            foreach (MethodInfo mi in match.Cast<MethodInfo>())
            {
                ParameterInfo[] parameters = mi.GetParameters();
                ParameterInfo firstParameter = parameters[0];
                bool argMatch = ReferenceEquals(firstParameter.ParameterType, _myArgType);
                bool returnMatch = ReferenceEquals(mi.ReturnType, _myReturnType);
                if (argMatch & returnMatch)
                {
                    return mi;
                }
            }
            return null;
        }
    }
}
