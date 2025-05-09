﻿using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Base.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// Represents a function call
    /// </summary>
    internal class FunctionCallElement : MemberElement
    {
        private readonly ArgumentList _myArguments;
        private readonly ICollection<MethodInfo> _myMethods;
        private CustomMethodInfo _myTargetMethodInfo;

        private Type _myOnDemandFunctionReturnType;
        public FunctionCallElement(string name, ArgumentList arguments)
        {
            MyName = name;
            _myArguments = arguments;
        }

        internal FunctionCallElement(string name, ICollection<MethodInfo> methods, ArgumentList arguments)
        {
            MyName = name;
            _myArguments = arguments;
            _myMethods = methods;
        }

        protected override void ResolveInternal()
        {
            // Get the types of our arguments
            Type[] argTypes = _myArguments.GetArgumentTypes();
            // Find all methods with our name on the type
            ICollection<MethodInfo> methods = _myMethods;

            if (methods == null)
            {
                // Convert member info to method info
                MemberInfo[] arr = GetMembers(MemberTypes.Method);
                MethodInfo[] arr2 = new MethodInfo[arr.Length];
                Array.Copy(arr, arr2, arr.Length);
                methods = arr2;
            }

            if (methods.Count > 0)
            {
                // More than one method exists with this name			
                BindToMethod(methods, MyPrevious, argTypes);
                return;
            }

            // No methods with this name exist; try to bind to an on-demand function
            _myOnDemandFunctionReturnType = MyContext.Variables.ResolveOnDemandFunction(MyName, argTypes);

            if (_myOnDemandFunctionReturnType == null)
            {
                // Failed to bind to a function
                ThrowFunctionNotFoundException(MyPrevious);
            }
        }

        private void ThrowFunctionNotFoundException(MemberElement previous)
        {
            if (previous == null)
            {
                ThrowCompileException(CompileErrorResourceKeys.UndefinedFunction, CompileExceptionReason.UndefinedName, MyName, _myArguments);
            }
            else
            {
                ThrowCompileException(CompileErrorResourceKeys.UndefinedFunctionOnType, CompileExceptionReason.UndefinedName, MyName, _myArguments, previous.TargetType.Name);
            }
        }

        private void ThrowNoAccessibleMethodsException(MemberElement previous)
        {
            if (previous == null)
            {
                ThrowCompileException(CompileErrorResourceKeys.NoAccessibleMatches, CompileExceptionReason.AccessDenied, MyName, _myArguments);
            }
            else
            {
                ThrowCompileException(CompileErrorResourceKeys.NoAccessibleMatchesOnType, CompileExceptionReason.AccessDenied, MyName, _myArguments, previous.TargetType.Name);
            }
        }

        private void ThrowAmbiguousMethodCallException()
        {
            ThrowCompileException(CompileErrorResourceKeys.AmbiguousCallOfFunction, CompileExceptionReason.AmbiguousMatch, MyName, _myArguments);
        }

        /// <summary>
        /// Try to find a match from a set of methods
        /// </summary>
        /// <param name="methods"></param>
        /// <param name="previous"></param>
        /// <param name="argTypes"></param>
        private void BindToMethod(ICollection<MethodInfo> methods, MemberElement previous, Type[] argTypes)
        {
            List<CustomMethodInfo> customInfos = new();

            // Wrap the MethodInfos in our custom class
            foreach (MethodInfo mi in methods)
            {
                CustomMethodInfo cmi = new(mi);
                customInfos.Add(cmi);
            }

            // Discard any methods that cannot qualify as overloads
            CustomMethodInfo[] arr = customInfos.ToArray();
            customInfos.Clear();

            foreach (CustomMethodInfo cmi in arr)
            {
                if (cmi.IsMatch(argTypes, MyPrevious, MyContext) == true)
                {
                    customInfos.Add(cmi);
                }
            }

            if (customInfos.Count == 0)
            {
                // We have no methods that can qualify as overloads; throw exception
                ThrowFunctionNotFoundException(previous);
            }
            else
            {
                // At least one method matches our criteria; do our custom overload resolution
                ResolveOverloads(customInfos.ToArray(), previous, argTypes);
            }
        }

        /// <summary>
        /// Find the best match from a set of overloaded methods
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="previous"></param>
        /// <param name="argTypes"></param>
        private void ResolveOverloads(CustomMethodInfo[] infos, MemberElement previous, Type[] argTypes)
        {
            // Compute a score for each candidate
            foreach (CustomMethodInfo cmi in infos)
            {
                cmi.ComputeScore(argTypes);
            }

            // Sort array from best to worst matches
            Array.Sort(infos);

            // Discard any matches that aren't accessible
            infos = GetAccessibleInfos(infos);

            // No accessible methods left
            if (infos.Length == 0)
            {
                ThrowNoAccessibleMethodsException(previous);
            }

            // Handle case where we have more than one match with the same score
            DetectAmbiguousMatches(infos);

            // If we get here, then there is only one best match
            _myTargetMethodInfo = infos[0];
        }

        private CustomMethodInfo[] GetAccessibleInfos(CustomMethodInfo[] infos)
        {
            List<CustomMethodInfo> accessible = new();

            foreach (CustomMethodInfo cmi in infos)
            {
                if (cmi.IsAccessible(this) == true)
                {
                    accessible.Add(cmi);
                }
            }

            return accessible.ToArray();
        }

        /// <summary>
        ///  Handle case where we have overloads with the same score
        /// </summary>
        /// <param name="infos"></param>
        private void DetectAmbiguousMatches(CustomMethodInfo[] infos)
        {
            List<CustomMethodInfo> sameScores = new();
            CustomMethodInfo first = infos[0];

            // Find all matches with the same score as the best match
            foreach (CustomMethodInfo cmi in infos)
            {
                if (((IEquatable<CustomMethodInfo>)cmi).Equals(first) == true)
                {
                    sameScores.Add(cmi);
                }
            }

            // More than one accessible match with the same score exists
            if (sameScores.Count > 1)
            {
                ThrowAmbiguousMethodCallException();
            }
        }

        protected override void Validate()
        {
            base.Validate();

            if (_myOnDemandFunctionReturnType != null)
            {
                return;
            }

            // Any function reference in an expression must return a value
            if (ReferenceEquals(Method.ReturnType, typeof(void)))
            {
                ThrowCompileException(CompileErrorResourceKeys.FunctionHasNoReturnValue, CompileExceptionReason.FunctionHasNoReturnValue, MyName);
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            base.Emit(ilg, services);

            ExpressionElement[] elements = _myArguments.ToArray();

            // If we are an on-demand function, then emit that and exit
            if (_myOnDemandFunctionReturnType != null)
            {
                EmitOnDemandFunction(elements, ilg, services);
                return;
            }

            bool isOwnerMember = MyOptions.IsOwnerType(Method.ReflectedType);

            // Load the owner if required
            if (MyPrevious == null && isOwnerMember == true && IsStatic == false)
            {
                EmitLoadOwner(ilg);
            }

            EmitFunctionCall(NextRequiresAddress, ilg, services);
        }

        private void EmitOnDemandFunction(ExpressionElement[] elements, FleeILGenerator ilg, IServiceProvider services)
        {
            // Load the variable collection
            EmitLoadVariables(ilg);
            // Load the function name
            ilg.Emit(OpCodes.Ldstr, MyName);
            // Load the arguments array
            EmitElementArrayLoad(elements, typeof(object), ilg, services);

            // Call the function to get the result
            MethodInfo mi = VariableCollection.GetFunctionInvokeMethod(_myOnDemandFunctionReturnType);

            EmitMethodCall(mi, ilg);
        }

        // Emit the arguments to a paramArray method call
        private void EmitParamArrayArguments(ParameterInfo[] parameters, ExpressionElement[] elements, FleeILGenerator ilg, IServiceProvider services)
        {
            // Get the fixed parameters
            ParameterInfo[] fixedParameters = new ParameterInfo[_myTargetMethodInfo.MyFixedArgTypes.Length];
            Array.Copy(parameters, fixedParameters, fixedParameters.Length);

            // Get the corresponding fixed parameters
            ExpressionElement[] fixedElements = new ExpressionElement[_myTargetMethodInfo.MyFixedArgTypes.Length];
            Array.Copy(elements, fixedElements, fixedElements.Length);

            // Emit the fixed arguments
            EmitRegularFunctionInternal(fixedParameters, fixedElements, ilg, services);

            // Get the paramArray arguments
            ExpressionElement[] paramArrayElements = new ExpressionElement[elements.Length - fixedElements.Length];
            Array.Copy(elements, fixedElements.Length, paramArrayElements, 0, paramArrayElements.Length);

            // Emit them into an array
            EmitElementArrayLoad(paramArrayElements, _myTargetMethodInfo.ParamArrayElementType, ilg, services);
        }

        /// <summary>
        /// Emit elements into an array
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="arrayElementType"></param>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        private static void EmitElementArrayLoad(ExpressionElement[] elements, Type arrayElementType, FleeILGenerator ilg, IServiceProvider services)
        {
            // Load the array length
            LiteralElement.EmitLoad(elements.Length, ilg);

            // Create the array
            ilg.Emit(OpCodes.Newarr, arrayElementType);

            // Store the new array in a unique local and remember the index
            LocalBuilder local = ilg.DeclareLocal(arrayElementType.MakeArrayType());
            int arrayLocalIndex = local.LocalIndex;
            Utility.EmitStoreLocal(ilg, arrayLocalIndex);

            for (int i = 0; i <= elements.Length - 1; i++)
            {
                // Load the array
                Utility.EmitLoadLocal(ilg, arrayLocalIndex);
                // Load the index
                LiteralElement.EmitLoad(i, ilg);
                // Emit the element (with any required conversions)
                ExpressionElement element = elements[i];
                element.Emit(ilg, services);
                ImplicitConverter.EmitImplicitConvert(element.ResultType, arrayElementType, ilg);
                // Store it into the array
                Utility.EmitArrayStore(ilg, arrayElementType);
            }

            // Load the array
            Utility.EmitLoadLocal(ilg, arrayLocalIndex);
        }

        public void EmitFunctionCall(bool nextRequiresAddress, FleeILGenerator ilg, IServiceProvider services)
        {
            ParameterInfo[] parameters = Method.GetParameters();
            ExpressionElement[] elements = _myArguments.ToArray();

            // Emit either a regular or paramArray call
            if (_myTargetMethodInfo.IsParamArray == false)
            {
                if (_myTargetMethodInfo.IsExtensionMethod == false)
                    EmitRegularFunctionInternal(parameters, elements, ilg, services);
                else
                    EmitExtensionFunctionInternal(parameters, elements, ilg, services);
            }
            else
            {
                EmitParamArrayArguments(parameters, elements, ilg, services);
            }

            EmitMethodCall(ResultType, nextRequiresAddress, Method, ilg);
        }

        private void EmitExtensionFunctionInternal(ParameterInfo[] parameters, ExpressionElement[] elements, FleeILGenerator ilg, IServiceProvider services)
        {
            Debug.Assert(parameters.Length == elements.Length + 1, "argument count mismatch");
            if (MyPrevious == null) EmitLoadOwner(ilg);
            //Emit each element and any required conversions to the actual parameter type
            for (int i = 1; i <= parameters.Length - 1; i++)
            {
                ExpressionElement element = elements[i - 1];
                ParameterInfo pi = parameters[i];
                element.Emit(ilg, services);
                bool success = ImplicitConverter.EmitImplicitConvert(element.ResultType, pi.ParameterType, ilg);
                Debug.Assert(success, "conversion failed");
            }
        }

        /// <summary>
        ///  Emit the arguments to a regular method call
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="elements"></param>
        /// <param name="ilg"></param>
        /// <param name="services"></param>
        private static void EmitRegularFunctionInternal(ParameterInfo[] parameters, ExpressionElement[] elements, FleeILGenerator ilg, IServiceProvider services)
        {
            Debug.Assert(parameters.Length == elements.Length, "argument count mismatch");

            // Emit each element and any required conversions to the actual parameter type
            for (int i = 0; i <= parameters.Length - 1; i++)
            {
                ExpressionElement element = elements[i];
                ParameterInfo pi = parameters[i];
                element.Emit(ilg, services);
                bool success = ImplicitConverter.EmitImplicitConvert(element.ResultType, pi.ParameterType, ilg);
                Debug.Assert(success, "conversion failed");
            }
        }

        /// <summary>
        /// The method info we will be calling
        /// </summary>	
        private MethodInfo Method => _myTargetMethodInfo.Target;

        public override Type ResultType
        {
            get
            {
                if (_myOnDemandFunctionReturnType != null)
                {
                    return _myOnDemandFunctionReturnType;
                }
                else
                {
                    return Method.ReturnType;
                }
            }
        }

        protected override bool RequiresAddress => !IsGetTypeMethod(Method);

        protected override bool IsPublic => Method.IsPublic;

        public override bool IsStatic => Method.IsStatic;
        public override bool IsExtensionMethod => _myTargetMethodInfo.IsExtensionMethod;
    }
}
