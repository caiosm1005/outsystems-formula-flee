using System.Globalization;
using System.Reflection;


namespace Flee.InternalTypes
{
    internal class BinaryOperatorBinder : CustomBinder
    {

        private readonly Type _myLeftType;
        private readonly Type _myRightType;
        private CustomBinder _customBinderImplementation;

        public BinaryOperatorBinder(Type leftType, Type rightType)
        {
            _myLeftType = leftType;
            _myRightType = rightType;
        }

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers,
            CultureInfo culture, string[] names, out object state)
        {
            return _customBinderImplementation.BindToMethod(bindingAttr, match, ref args, modifiers, culture, names, out state);
        }

        public override System.Reflection.MethodBase SelectMethod(System.Reflection.BindingFlags bindingAttr, System.Reflection.MethodBase[] match, System.Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            foreach (MethodInfo mi in match)
            {
                ParameterInfo[] parameters = mi.GetParameters();
                bool leftValid = ImplicitConverter.EmitImplicitConvert(_myLeftType, parameters[0].ParameterType, null);
                bool rightValid = ImplicitConverter.EmitImplicitConvert(_myRightType, parameters[1].ParameterType, null);

                if (leftValid == true & rightValid == true)
                {
                    return mi;
                }
            }
            return null;
        }
    }
}