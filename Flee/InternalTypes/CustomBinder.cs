using System.Reflection;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Base reflection <see cref="Binder"/> that no-ops every contract method, so subclasses
    /// only need to override the ones they actually use (typically just
    /// <see cref="Binder.SelectMethod"/>).
    /// </summary>
    internal abstract class CustomBinder : Binder
    {
        /// <summary>
        /// Not implemented; binding to fields isn't needed by Flee's emitters.
        /// </summary>
        /// <param name="bindingAttr">Ignored.</param>
        /// <param name="match">Ignored.</param>
        /// <param name="value">Ignored.</param>
        /// <param name="culture">Ignored.</param>
        /// <returns>Always <see langword="null"/>.</returns>
        public override FieldInfo BindToField(
            BindingFlags bindingAttr,
            FieldInfo[] match,
            object? value,
            System.Globalization.CultureInfo? culture)
        {
            return null!;
        }

        /// <summary>
        /// Not implemented; type changes aren't needed by Flee's emitters.
        /// </summary>
        /// <param name="value">Ignored.</param>
        /// <param name="type">Ignored.</param>
        /// <param name="culture">Ignored.</param>
        /// <returns>Always <see langword="null"/>.</returns>
        public override object ChangeType(object value, Type type, System.Globalization.CultureInfo? culture)
        {
            return null!;
        }

        /// <summary>
        /// No-op; argument reordering isn't needed by Flee's emitters.
        /// </summary>
        /// <param name="args">Ignored.</param>
        /// <param name="state">Ignored.</param>
        public override void ReorderArgumentArray(ref object?[] args, object state)
        {
        }

        /// <summary>
        /// Not implemented; property selection isn't needed by Flee's emitters.
        /// </summary>
        /// <param name="bindingAttr">Ignored.</param>
        /// <param name="match">Ignored.</param>
        /// <param name="returnType">Ignored.</param>
        /// <param name="indexes">Ignored.</param>
        /// <param name="modifiers">Ignored.</param>
        /// <returns>Always <see langword="null"/>.</returns>
        public override PropertyInfo? SelectProperty(
            BindingFlags bindingAttr,
            PropertyInfo[] match,
            Type? returnType,
            Type[]? indexes,
            ParameterModifier[]? modifiers)
        {
            return null;
        }
    }
}
