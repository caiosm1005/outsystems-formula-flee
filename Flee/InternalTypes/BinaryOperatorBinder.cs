using System.Globalization;
using System.Reflection;

namespace Flee.InternalTypes
{
    /// <summary>
    /// A reflection <see cref="Binder"/> that picks the overload of a binary operator method
    /// whose two parameters can be reached from <paramref name="leftType"/> and
    /// <paramref name="rightType"/> via implicit conversions.
    /// </summary>
    /// <param name="leftType">The CLR type of the left operand.</param>
    /// <param name="rightType">The CLR type of the right operand.</param>
    internal class BinaryOperatorBinder(Type leftType, Type rightType) : CustomBinder
    {
        private readonly Type _myLeftType = leftType;
        private readonly Type _myRightType = rightType;

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
        /// Returns the first method in <paramref name="match"/> whose parameter types both accept
        /// the configured operand types via implicit conversion.
        /// </summary>
        /// <param name="bindingAttr">Ignored.</param>
        /// <param name="match">The candidate methods.</param>
        /// <param name="types">Ignored; the binder uses its stored operand types.</param>
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
                bool leftValid = ImplicitConverter.EmitImplicitConvert(
                    _myLeftType,
                    parameters[0].ParameterType,
                    null);
                bool rightValid = ImplicitConverter.EmitImplicitConvert(
                    _myRightType,
                    parameters[1].ParameterType,
                    null);

                if (leftValid & rightValid)
                {
                    return mi;
                }
            }
            return null;
        }
    }
}
