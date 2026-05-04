using System.Reflection;

namespace Flee.InternalTypes
{
    internal abstract class CustomBinder : Binder
    {

        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object? value, System.Globalization.CultureInfo? culture)
        {
            return null!;
        }

        public override object ChangeType(object value, Type type, System.Globalization.CultureInfo? culture)
        {
            return null!;
        }


        public override void ReorderArgumentArray(ref object?[] args, object state)
        {
        }

        public override PropertyInfo? SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type? returnType, Type[]? indexes, ParameterModifier[]? modifiers)
        {
            return null;
        }
    }
}
