using System.Globalization;
using System.Reflection;

namespace Flee.InternalTypes
{
    internal class ExplicitOperatorMethodBinder(Type returnType, Type argType) : CustomBinder
    {
        private readonly Type _myReturnType = returnType;
        private readonly Type _myArgType = argType;

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object?[] args, ParameterModifier[]? modifiers,
            CultureInfo? culture, string[]? names, out object? state)
        {
            state = null;
            return null!;
        }

        public override MethodBase? SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[]? modifiers)
        {
            foreach (MethodInfo mi in match.Cast<MethodInfo>())
            {
                ParameterInfo[] parameters = mi.GetParameters();
                ParameterInfo firstParameter = parameters[0];
                if (ReferenceEquals(firstParameter.ParameterType, _myArgType) & ReferenceEquals(mi.ReturnType, _myReturnType))
                {
                    return mi;
                }
            }
            return null;
        }
    }
}
