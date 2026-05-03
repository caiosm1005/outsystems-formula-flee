using System.Globalization;
using System.Reflection;

namespace Flee.InternalTypes
{
    internal class ExplicitOperatorMethodBinder : CustomBinder
    {
        private readonly Type _myReturnType;
        private readonly Type _myArgType;

        public ExplicitOperatorMethodBinder(Type returnType, Type argType)
        {
            _myReturnType = returnType;
            _myArgType = argType;
        }

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object?[] args, ParameterModifier[]? modifiers,
            CultureInfo? culture, string[]? names, out object? state)
        {
            state = null;
            return null!;
        }

        public override System.Reflection.MethodBase? SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[]? modifiers)
        {
            foreach (MethodInfo mi in match)
            {
                ParameterInfo[] parameters = mi.GetParameters();
                ParameterInfo firstParameter = parameters[0];
                if (object.ReferenceEquals(firstParameter.ParameterType, _myArgType) & object.ReferenceEquals(mi.ReturnType, _myReturnType))
                {
                    return mi;
                }
            }
            return null;
        }
    }
}
