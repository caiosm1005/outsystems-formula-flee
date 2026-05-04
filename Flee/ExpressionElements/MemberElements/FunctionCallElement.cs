using System.Diagnostics;
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
    /// Represents a function call. Performs custom overload resolution against a candidate
    /// method set, falls back to on-demand variable-collection functions, and emits the
    /// chosen method's call (including <see cref="ParamArrayAttribute"/> and extension-method
    /// shapes).
    /// </summary>
    internal class FunctionCallElement : MemberElement
    {
        private readonly ArgumentList _myArguments;
        private readonly ICollection<MethodInfo>? _myMethods;
        private CustomMethodInfo _myTargetMethodInfo = null!;
        private Type? _myOnDemandFunctionReturnType;

        /// <summary>
        /// Initializes a new instance with the parsed call site.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="arguments">The argument list.</param>
        public FunctionCallElement(string name, ArgumentList arguments)
        {
            MyName = name;
            _myArguments = arguments;
        }

        /// <summary>
        /// Initializes a new instance with a pre-resolved candidate method set. Used when the
        /// caller has already narrowed candidates (e.g. an indexer property's getter).
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="methods">The candidate methods.</param>
        /// <param name="arguments">The argument list.</param>
        internal FunctionCallElement(string name, ICollection<MethodInfo> methods, ArgumentList arguments)
        {
            MyName = name;
            _myArguments = arguments;
            _myMethods = methods;
        }

        /// <summary>
        /// Resolves the call: picks an overload from the candidate set, or falls back to
        /// <see cref="VariableCollection.ResolveOnDemandFunction"/> when no method matches.
        /// </summary>
        protected override void ResolveInternal()
        {
            // Get the types of our arguments
            Type[] argTypes = _myArguments.GetArgumentTypes();
            // Find all methods with our name on the type
            ICollection<MethodInfo>? methods = _myMethods;

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

        /// <summary>
        /// Throws "undefined function" with the appropriate message based on whether there's
        /// a predecessor in the dereference chain.
        /// </summary>
        /// <param name="previous">The predecessor in the dereference chain.</param>
        private void ThrowFunctionNotFoundException(MemberElement? previous)
        {
            if (previous == null)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.UndefinedFunction,
                    CompileExceptionReason.UndefinedName,
                    MyName,
                    _myArguments);
            }
            else
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.UndefinedFunctionOnType,
                    CompileExceptionReason.UndefinedName,
                    MyName,
                    _myArguments,
                    previous.TargetType.Name);
            }
        }

        /// <summary>
        /// Throws "no accessible matches" with the appropriate message based on whether there's
        /// a predecessor in the dereference chain.
        /// </summary>
        /// <param name="previous">The predecessor in the dereference chain.</param>
        private void ThrowNoAccessibleMethodsException(MemberElement? previous)
        {
            if (previous == null)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.NoAccessibleMatches,
                    CompileExceptionReason.AccessDenied,
                    MyName,
                    _myArguments);
            }
            else
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.NoAccessibleMatchesOnType,
                    CompileExceptionReason.AccessDenied,
                    MyName,
                    _myArguments,
                    previous.TargetType.Name);
            }
        }

        /// <summary>
        /// Throws "ambiguous call" for the resolved candidate set.
        /// </summary>
        private void ThrowAmbiguousMethodCallException()
        {
            ThrowCompileException(
                CompileErrorResourceKeys.AmbiguousCallOfFunction,
                CompileExceptionReason.AmbiguousMatch,
                MyName,
                _myArguments);
        }

        /// <summary>
        /// Try to find a match from a set of methods. Wraps each method in a
        /// <see cref="CustomMethodInfo"/>, filters non-callable candidates, then runs overload
        /// resolution.
        /// </summary>
        /// <param name="methods">The candidate methods.</param>
        /// <param name="previous">The predecessor in the dereference chain.</param>
        /// <param name="argTypes">The actual argument types.</param>
        private void BindToMethod(ICollection<MethodInfo> methods, MemberElement? previous, Type[] argTypes)
        {
            List<CustomMethodInfo> customInfos = [];

            // Wrap the MethodInfos in our custom class
            foreach (MethodInfo mi in methods)
            {
                CustomMethodInfo cmi = new(mi);
                customInfos.Add(cmi);
            }

            // Discard any methods that cannot qualify as overloads
            CustomMethodInfo[] arr = [.. customInfos];
            customInfos.Clear();

            foreach (CustomMethodInfo cmi in arr)
            {
                if (cmi.IsMatch(argTypes, MyPrevious, MyContext))
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
                ResolveOverloads([.. customInfos], previous, argTypes);
            }
        }

        /// <summary>
        /// Find the best match from a set of overloaded methods by scoring, sorting, and
        /// detecting ambiguous ties.
        /// </summary>
        /// <param name="infos">The candidate methods.</param>
        /// <param name="previous">The predecessor in the dereference chain.</param>
        /// <param name="argTypes">The actual argument types.</param>
        private void ResolveOverloads(CustomMethodInfo[] infos, MemberElement? previous, Type[] argTypes)
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

        /// <summary>
        /// Filters <paramref name="infos"/> down to candidates whose declaring members are
        /// accessible from this expression.
        /// </summary>
        /// <param name="infos">The candidate set.</param>
        /// <returns>The accessible subset.</returns>
        private CustomMethodInfo[] GetAccessibleInfos(CustomMethodInfo[] infos)
        {
            List<CustomMethodInfo> accessible = [];

            foreach (CustomMethodInfo cmi in infos)
            {
                if (cmi.IsAccessible(this))
                {
                    accessible.Add(cmi);
                }
            }

            return [.. accessible];
        }

        /// <summary>
        /// Handle case where we have overloads with the same score. Throws "ambiguous call"
        /// when more than one candidate ties for the top score.
        /// </summary>
        /// <param name="infos">The candidate set, sorted best-first.</param>
        private void DetectAmbiguousMatches(CustomMethodInfo[] infos)
        {
            List<CustomMethodInfo> sameScores = [];
            CustomMethodInfo first = infos[0];

            // Find all matches with the same score as the best match
            foreach (CustomMethodInfo cmi in infos)
            {
                if (((IEquatable<CustomMethodInfo>)cmi).Equals(first))
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

        /// <summary>
        /// Validates that the resolved method has a return value (functions used in expressions
        /// must produce a value).
        /// </summary>
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
                ThrowCompileException(
                    CompileErrorResourceKeys.FunctionHasNoReturnValue,
                    CompileExceptionReason.FunctionHasNoReturnValue,
                    MyName);
            }
        }

        /// <summary>
        /// Emits the call. On-demand functions go through the variable-collection helper;
        /// regular calls load the owner first when needed and then dispatch via
        /// <see cref="EmitFunctionCall"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
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

            bool isOwnerMember = Method.ReflectedType != null
                && MyOptions.IsOwnerType(Method.ReflectedType);

            // Load the owner if required
            if (MyPrevious == null && isOwnerMember && !IsStatic)
            {
                EmitLoadOwner(ilg);
            }

            EmitFunctionCall(NextRequiresAddress, ilg, services);
        }

        /// <summary>
        /// Emits an on-demand function call: loads the variable collection, name, and an
        /// object-array of arguments, then calls the closed-generic helper.
        /// </summary>
        /// <param name="elements">The argument elements.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitOnDemandFunction(
            ExpressionElement[] elements,
            FleeILGenerator ilg,
            IServiceProvider services)
        {
            // Load the variable collection
            EmitLoadVariables(ilg);
            // Load the function name
            ilg.Emit(OpCodes.Ldstr, MyName);
            // Load the arguments array
            EmitElementArrayLoad(elements, typeof(object), ilg, services);

            // Call the function to get the result
            MethodInfo mi = VariableCollection.GetFunctionInvokeMethod(_myOnDemandFunctionReturnType!);

            EmitMethodCall(mi, ilg);
        }

        /// <summary>
        /// Emit the arguments to a paramArray method call: regular arguments first, then a
        /// freshly built array of the trailing arguments.
        /// </summary>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="elements">The argument elements.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitParamArrayArguments(
            ParameterInfo[] parameters,
            ExpressionElement[] elements,
            FleeILGenerator ilg,
            IServiceProvider services)
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
            ExpressionElement[] paramArrayElements =
                new ExpressionElement[elements.Length - fixedElements.Length];
            Array.Copy(elements, fixedElements.Length, paramArrayElements, 0, paramArrayElements.Length);

            // Emit them into an array
            EmitElementArrayLoad(
                paramArrayElements,
                _myTargetMethodInfo.ParamArrayElementType!,
                ilg,
                services);
        }

        /// <summary>
        /// Emit elements into a fresh array of <paramref name="arrayElementType"/>, leaving
        /// the array reference on the stack.
        /// </summary>
        /// <param name="elements">The element values.</param>
        /// <param name="arrayElementType">The CLR type of the array element.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private static void EmitElementArrayLoad(
            ExpressionElement[] elements,
            Type arrayElementType,
            FleeILGenerator ilg,
            IServiceProvider services)
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
                _ = ImplicitConverter.EmitImplicitConvert(element.ResultType, arrayElementType, ilg);
                // Store it into the array
                Utility.EmitArrayStore(ilg, arrayElementType);
            }

            // Load the array
            Utility.EmitLoadLocal(ilg, arrayLocalIndex);
        }

        /// <summary>
        /// Public-from-the-assembly emit entry point used by <see cref="IndexerElement"/>
        /// when it routes indexer-property dispatch through this element.
        /// </summary>
        /// <param name="nextRequiresAddress">Whether the next link wants the result's address.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public void EmitFunctionCall(bool nextRequiresAddress, FleeILGenerator ilg, IServiceProvider services)
        {
            ParameterInfo[] parameters = Method.GetParameters();
            ExpressionElement[] elements = _myArguments.ToArray();

            // Emit either a regular or paramArray call
            if (!_myTargetMethodInfo.IsParamArray)
            {
                if (!_myTargetMethodInfo.IsExtensionMethod)
                {
                    EmitRegularFunctionInternal(parameters, elements, ilg, services);
                }
                else
                {
                    EmitExtensionFunctionInternal(parameters, elements, ilg, services);
                }
            }
            else
            {
                EmitParamArrayArguments(parameters, elements, ilg, services);
            }

            EmitMethodCall(ResultType, nextRequiresAddress, Method, ilg);
        }

        /// <summary>
        /// Emit the receiver and arguments for an extension-method call: the implicit receiver
        /// (predecessor or owner) takes parameter slot 0 and the explicit arguments fill the rest.
        /// </summary>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="elements">The argument elements.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitExtensionFunctionInternal(
            ParameterInfo[] parameters,
            ExpressionElement[] elements,
            FleeILGenerator ilg,
            IServiceProvider services)
        {
            Debug.Assert(parameters.Length == elements.Length + 1, "argument count mismatch");
            if (MyPrevious == null)
            {
                EmitLoadOwner(ilg);
            }
            //Emit each element and any required conversions to the actual parameter type
            for (int i = 1; i <= parameters.Length - 1; i++)
            {
                ExpressionElement element = elements[i - 1];
                ParameterInfo pi = parameters[i];
                element.Emit(ilg, services);
                bool success = ImplicitConverter.EmitImplicitConvert(
                    element.ResultType,
                    pi.ParameterType,
                    ilg);
                Debug.Assert(success, "conversion failed");
            }
        }

        /// <summary>
        /// Emit the arguments to a regular method call.
        /// </summary>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="elements">The argument elements.</param>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitRegularFunctionInternal(
            ParameterInfo[] parameters,
            ExpressionElement[] elements,
            FleeILGenerator ilg,
            IServiceProvider services)
        {
            Debug.Assert(parameters.Length == elements.Length, "argument count mismatch");

            // Emit each element and any required conversions to the actual parameter type
            for (int i = 0; i <= parameters.Length - 1; i++)
            {
                ExpressionElement element = elements[i];
                ParameterInfo pi = parameters[i];
                element.Emit(ilg, services);
                bool success = ImplicitConverter.EmitImplicitConvert(
                    element.ResultType,
                    pi.ParameterType,
                    ilg);
                Debug.Assert(success, "conversion failed");
            }
        }

        /// <summary>
        /// Gets the resolved method that will be called.
        /// </summary>
        private MethodInfo Method => _myTargetMethodInfo.Target;

        /// <summary>
        /// Gets the function's return type, or the on-demand declared return type when applicable.
        /// </summary>
        public override Type ResultType => _myOnDemandFunctionReturnType ?? Method.ReturnType;

        /// <summary>
        /// Method calls produce addressable results except for <see cref="object.GetType"/>.
        /// </summary>
        protected override bool RequiresAddress => !IsGetTypeMethod(Method);

        /// <summary>
        /// Reports whether the resolved method is public.
        /// </summary>
        protected override bool IsPublic => Method.IsPublic;

        /// <summary>
        /// Reports whether the resolved method is static.
        /// </summary>
        public override bool IsStatic => Method.IsStatic;

        /// <summary>
        /// Reports whether overload resolution chose an extension-method candidate.
        /// </summary>
        public override bool IsExtensionMethod => _myTargetMethodInfo.IsExtensionMethod;
    }
}
