﻿using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal enum BinaryArithmeticOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Mod,
        Power
    }

    internal enum LogicalCompareOperation
    {
        LessThan,
        GreaterThan,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual
    }

    internal enum AndOrOperation
    {
        And,
        Or
    }

    internal enum ShiftOperation
    {
        LeftShift,
        RightShift
    }

    internal delegate T ExpressionEvaluator<T>(object owner, ExpressionContext context, VariableCollection variables);

    internal abstract class CustomBinder : Binder
    {

        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
        {
            return null;
        }

        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
        {
            state = null;
            return null;
        }

        public override object ChangeType(object value, Type type, CultureInfo culture)
        {
            return null;
        }


        public override void ReorderArgumentArray(ref object[] args, object state)
        {
        }

        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
        {
            return null;
        }
    }

    internal class ExplicitOperatorMethodBinder : CustomBinder
    {
        private readonly Type _myReturnType;
        private readonly Type _myArgType;
        
        public ExplicitOperatorMethodBinder(Type returnType, Type argType)
        {
            _myReturnType = returnType;
            _myArgType = argType;
        }

        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
        {
            foreach (MethodInfo mi in match)
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

    internal class BinaryOperatorBinder : CustomBinder
    {

        private readonly Type _myLeftType;
        private readonly Type _myRightType;

        public BinaryOperatorBinder(Type leftType, Type rightType)
        {
            _myLeftType = leftType;
            _myRightType = rightType;
        }

        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
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

    internal class Null
    {

    }

    internal class DefaultExpressionOwner
    {


        private static readonly DefaultExpressionOwner OurInstance = new();

        private DefaultExpressionOwner()
        {
        }

        public static object Instance => OurInstance;
    }

    /// <summary>
    /// Helper class to resolve overloads
    /// </summary>
    internal class CustomMethodInfo : IComparable<CustomMethodInfo>, IEquatable<CustomMethodInfo>
    {
        /// <summary>
        /// Method we are wrapping
        /// </summary>
        private readonly MethodInfo _myTarget;
        /// <summary>
        /// The rating of how close the method matches the given arguments (0 is best)
        /// </summary>
        private float _myScore;
        public bool IsParamArray;
        public Type[] MyFixedArgTypes;
        public Type[] MyParamArrayArgTypes;
        public bool IsExtensionMethod;
        public Type ParamArrayElementType;
        public CustomMethodInfo(MethodInfo target)
        {
            _myTarget = target;
        }

        public void ComputeScore(Type[] argTypes)
        {
            ParameterInfo[] @params = _myTarget.GetParameters();

            if (@params.Length == 0)
            {
                _myScore = 0.0F;
            }
            else if (@params.Length == 1 && argTypes.Length == 0)//extension method without parameter support -> prefer members
            {
                _myScore = 0.1F;
            }
            else if (IsParamArray == true)
            {
                _myScore = ComputeScoreForParamArray(@params, argTypes);
            }
            else if (IsExtensionMethod == true)
            {
                _myScore = ComputeScoreExtensionMethodInternal(@params, argTypes);
            }
            else
            {
                _myScore = ComputeScoreInternal(@params, argTypes);
            }
        }

        /// <summary>
        /// Compute a score showing how close our method matches the given argument types (for extension methods)
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="argTypes"></param>
        /// <returns></returns>
        private static float ComputeScoreExtensionMethodInternal(ParameterInfo[] parameters, Type[] argTypes)
        {
            Debug.Assert(parameters.Length == argTypes.Length + 1);
            int sum = 0;

            for (int i = 0; i <= argTypes.Length - 1; i++)
            {
                sum += ImplicitConverter.GetImplicitConvertScore(argTypes[i], parameters[i + 1].ParameterType);
            }

            return sum;
        }

        /// <summary>
        /// Compute a score showing how close our method matches the given argument types
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="argTypes"></param>
        /// <returns></returns>
        private static float ComputeScoreInternal(ParameterInfo[] parameters, Type[] argTypes)
        {
            // Our score is the average of the scores of each parameter.  The lower the score, the better the match.
            int sum = ComputeSum(parameters, argTypes);

            return (float)sum / (float)argTypes.Length;
        }

        private static int ComputeSum(ParameterInfo[] parameters, Type[] argTypes)
        {
            Debug.Assert(parameters.Length == argTypes.Length);
            int sum = 0;

            for (int i = 0; i <= parameters.Length - 1; i++)
            {
                sum += ImplicitConverter.GetImplicitConvertScore(argTypes[i], parameters[i].ParameterType);
            }

            return sum;
        }

        private float ComputeScoreForParamArray(ParameterInfo[] parameters, Type[] argTypes)
        {
            ParameterInfo paramArrayParameter = parameters[parameters.Length - 1];
            int fixedParameterCount = paramArrayParameter.Position;

            ParameterInfo[] fixedParameters = new ParameterInfo[fixedParameterCount];

            Array.Copy(parameters, fixedParameters, fixedParameterCount);

            int fixedSum = ComputeSum(fixedParameters, MyFixedArgTypes);

            Type paramArrayElementType = paramArrayParameter.ParameterType.GetElementType();

            int paramArraySum = 0;

            foreach (Type argType in MyParamArrayArgTypes)
            {
                paramArraySum += ImplicitConverter.GetImplicitConvertScore(argType, paramArrayElementType);
            }

            float score;

            if (argTypes.Length > 0)
            {
                score = (fixedSum + paramArraySum) / argTypes.Length;
            }
            else
            {
                score = 0;
            }

            // The param array score gets a slight penalty so that it scores worse than direct matches
            return score + 1;
        }

        public bool IsAccessible(MemberElement owner)
        {
            return owner.IsMemberAccessible(_myTarget);
        }

        /// <summary>
        /// Is the given MethodInfo usable as an overload?
        /// </summary>
        /// <param name="argTypes"></param>
        /// <returns></returns>
        public bool IsMatch(Type[] argTypes, MemberElement previous, ExpressionContext context)
        {
            ParameterInfo[] parameters = _myTarget.GetParameters();

            // If there are no parameters and no arguments were passed, then we are a match.
            if (parameters.Length == 0 & argTypes.Length == 0)
            {
                return true;
            }

            // If there are no parameters but there are arguments, we cannot be a match
            if (parameters.Length == 0 & argTypes.Length > 0)
            {
                return false;
            }

            // Is the last parameter a paramArray?
            ParameterInfo lastParam = parameters[parameters.Length - 1];

            if (lastParam.IsDefined(typeof(ParamArrayAttribute), false) == false)
            {
                //Extension method support
                if (parameters.Length == argTypes.Length + 1)
                {
                    IsExtensionMethod = true;
                    return AreValidExtensionMethodArgumentsForParameters(argTypes, parameters, previous, context);
                }
                if (parameters.Length != argTypes.Length)
                {
                    // Not a paramArray and parameter and argument counts don't match
                    return false;
                }
                else
                {
                    // Regular method call, do the test
                    return AreValidArgumentsForParameters(argTypes, parameters);
                }
            }

            // At this point, we are dealing with a paramArray call

            // If the parameter and argument counts are equal and there is an implicit conversion from one to the other, we are a match.
            if (parameters.Length == argTypes.Length && AreValidArgumentsForParameters(argTypes, parameters) == true)
            {
                return true;
            }
            else if (IsParamArrayMatch(argTypes, parameters, lastParam) == true)
            {
                IsParamArray = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsParamArrayMatch(Type[] argTypes, ParameterInfo[] parameters, ParameterInfo paramArrayParameter)
        {
            // Get the count of arguments before the paramArray parameter
            int fixedParameterCount = paramArrayParameter.Position;
            Type[] fixedArgTypes = new Type[fixedParameterCount];
            ParameterInfo[] fixedParameters = new ParameterInfo[fixedParameterCount];

            // Get the argument types and parameters before the paramArray
            Array.Copy(argTypes, fixedArgTypes, fixedParameterCount);
            Array.Copy(parameters, fixedParameters, fixedParameterCount);

            // If the fixed arguments don't match, we are not a match
            if (AreValidArgumentsForParameters(fixedArgTypes, fixedParameters) == false)
            {
                return false;
            }

            // Get the type of the paramArray
            ParamArrayElementType = paramArrayParameter.ParameterType.GetElementType();

            // Get the types of the arguments passed to the paramArray
            Type[] paramArrayArgTypes = new Type[argTypes.Length - fixedParameterCount];
            Array.Copy(argTypes, fixedParameterCount, paramArrayArgTypes, 0, paramArrayArgTypes.Length);

            // Check each argument
            foreach (Type argType in paramArrayArgTypes)
            {
                if (ImplicitConverter.EmitImplicitConvert(argType, ParamArrayElementType, null) == false)
                {
                    return false;
                }
            }

            MyFixedArgTypes = fixedArgTypes;
            MyParamArrayArgTypes = paramArrayArgTypes;

            // They all match, so we are a match
            return true;
        }

        private static bool AreValidExtensionMethodArgumentsForParameters(Type[] argTypes, ParameterInfo[] parameters, MemberElement previous, ExpressionContext context)
        {
            Debug.Assert(argTypes.Length + 1 == parameters.Length);

            if (previous != null)
            {
                if (ImplicitConverter.EmitImplicitConvert(previous.ResultType, parameters[0].ParameterType, null) == false)
                {
                    return false;
                }
            }
            else if (context.ExpressionOwner != null)
            {
                if (ImplicitConverter.EmitImplicitConvert(context.ExpressionOwner.GetType(), parameters[0].ParameterType, null) == false)
                    return false;
            }
            else
                return false;

            //Match if every given argument is implicitly convertible to the method's corresponding parameter
            for (int i = 0; i <= argTypes.Length - 1; i++)
            {
                if (ImplicitConverter.EmitImplicitConvert(argTypes[i], parameters[i + 1].ParameterType, null) == false)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AreValidArgumentsForParameters(Type[] argTypes, ParameterInfo[] parameters)
        {
            Debug.Assert(argTypes.Length == parameters.Length);
            // Match if every given argument is implicitly convertible to the method's corresponding parameter
            for (int i = 0; i <= argTypes.Length - 1; i++)
            {
                if (ImplicitConverter.EmitImplicitConvert(argTypes[i], parameters[i].ParameterType, null) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public int CompareTo(CustomMethodInfo other)
        {
            return _myScore.CompareTo(other._myScore);
        }

        private bool Equals1(CustomMethodInfo other)
        {
            return _myScore == other._myScore;
        }
        bool IEquatable<CustomMethodInfo>.Equals(CustomMethodInfo other)
        {
            return Equals1(other);
        }

        public MethodInfo Target => _myTarget;
    }

    internal class ShortCircuitInfo
    {

        public Stack Operands;
        public Stack Operators;
        private Dictionary<object, Label> Labels;

        public ShortCircuitInfo()
        {
            Operands = new Stack();
            Operators = new Stack();
            Labels = new Dictionary<object, Label>();
        }

        public void ClearTempState()
        {
            Operands.Clear();
            Operators.Clear();
        }

        public Label AddLabel(object key, Label lbl)
        {
            Labels.Add(key, lbl);
            return lbl;
        }

        public bool HasLabel(object key)
        {
            return Labels.ContainsKey(key);
        }

        public Label FindLabel(object key)
        {
            return Labels[key];
        }
    }

    /// <summary>
    /// Wraps an expression element so that it is loaded from a local slot
    /// </summary>
    internal class LocalBasedElement : ExpressionElement
    {
        private readonly int _myIndex;

        private readonly ExpressionElement _myTarget;
        public LocalBasedElement(ExpressionElement target, int index)
        {
            _myTarget = target;
            _myIndex = index;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            Utility.EmitLoadLocal(ilg, _myIndex);
        }

        public override Type ResultType => _myTarget.ResultType;
    }

    /// <summary>
    /// Helper class for storing strongly-typed properties
    /// </summary>
    internal class PropertyDictionary
    {
        private readonly Dictionary<string, object> _myProperties;
        public PropertyDictionary()
        {
            _myProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public PropertyDictionary Clone()
        {
            PropertyDictionary copy = new();

            foreach (KeyValuePair<string, object> pair in _myProperties)
            {
                copy.SetValue(pair.Key, pair.Value);
            }

            return copy;
        }

        public T GetValue<T>(string name)
        {
            object value;
            if (_myProperties.TryGetValue(name, out value) == false)
            {
                Debug.Fail($"Unknown property '{name}'");
            }
            return (T)value;
        }

        public void SetToDefault<T>(string name)
        {
            T value = default(T);
            SetValue(name, value);
        }

        public void SetValue(string name, object value)
        {
            _myProperties[name] = value;
        }

        public bool Contains(string name)
        {
            return _myProperties.ContainsKey(name);
        }
    }
}
