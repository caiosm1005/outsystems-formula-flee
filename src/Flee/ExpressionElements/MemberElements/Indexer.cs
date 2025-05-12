using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;


namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// Element representing an array index
    /// </summary>
    internal class IndexerElement : MemberElement
    {
        private ExpressionElement _myIndexerElement;

        private readonly ArgumentList _myIndexerElements;
        public IndexerElement(ArgumentList indexer)
        {
            _myIndexerElements = indexer;
        }

        protected override void ResolveInternal()
        {
            // Are we are indexing on an array?
            Type target = MyPrevious.TargetType;

            // Yes, so setup for an array index
            if (target.IsArray)
            {
                SetupArrayIndexer();
                return;
            }

            // Not an array, so try to find an indexer on the type
            if (!FindIndexer(target))
            {
                ThrowCompileException(CompileErrorResourceKeys.TypeNotArrayAndHasNoIndexerOfType, CompileExceptionReason.TypeMismatch, target.Name, _myIndexerElements);
            }
        }

        private void SetupArrayIndexer()
        {
            _myIndexerElement = _myIndexerElements[0];

            if (_myIndexerElements.Count > 1)
            {
                ThrowCompileException(CompileErrorResourceKeys.MultiArrayIndexNotSupported, CompileExceptionReason.TypeMismatch);
            }
            else if (!ImplicitConverter.EmitImplicitConvert(_myIndexerElement.ResultType, typeof(int), null, null))
            {
                ThrowCompileException(CompileErrorResourceKeys.ArrayIndexersMustBeOfType, CompileExceptionReason.TypeMismatch, typeof(int).Name);
            }
        }

        private bool FindIndexer(Type targetType)
        {
            // Get the default members
            MemberInfo[] members = targetType.GetDefaultMembers();

            List<MethodInfo> methods = new();

            // Use the first one that's valid for our indexer type
            foreach (MemberInfo mi in members)
            {
                PropertyInfo pi = mi as PropertyInfo;
                if (pi != null)
                {
                    methods.Add(pi.GetGetMethod(true));
                }
            }

            FunctionCallElement func = new("Indexer", methods.ToArray(), _myIndexerElements);
            func.Resolve(MyServices);
            _myIndexerElement = func;

            return true;
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            base.Emit(ilg, services);

            if (IsArray)
            {
                EmitArrayLoad(ilg, services);
            }
            else
            {
                EmitIndexer(ilg, services);
            }
        }

        private void EmitArrayLoad(FleeILGenerator ilg, IServiceProvider services)
        {
            _myIndexerElement.Emit(ilg, services);
            ImplicitConverter.EmitImplicitConvert(_myIndexerElement.ResultType, typeof(int), ilg, services);

            Type elementType = ResultType;

            if (!elementType.IsValueType)
            {
                // Simple reference load
                ilg.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                EmitValueTypeArrayLoad(ilg, elementType);
            }
        }

        private void EmitValueTypeArrayLoad(FleeILGenerator ilg, Type elementType)
        {
            if (NextRequiresAddress)
            {
                ilg.Emit(OpCodes.Ldelema, elementType);
            }
            else
            {
                Utility.EmitArrayLoad(ilg, elementType);
            }
        }

        private void EmitIndexer(FleeILGenerator ilg, IServiceProvider services)
        {
            FunctionCallElement func = (FunctionCallElement)_myIndexerElement;
            func.EmitFunctionCall(NextRequiresAddress, ilg, services);
        }

        private Type ArrayType
        {
            get
            {
                if (IsArray)
                {
                    return MyPrevious.TargetType;
                }
                else
                {
                    return null;
                }
            }
        }

        private bool IsArray => MyPrevious.TargetType.IsArray;

        protected override bool RequiresAddress => !IsArray;

        public override Type ResultType
        {
            get
            {
                if (IsArray)
                {
                    return ArrayType.GetElementType();
                }
                else
                {
                    return _myIndexerElement.ResultType;
                }
            }
        }

        protected override bool IsPublic
        {
            get
            {
                if (IsArray)
                {
                    return true;
                }
                else
                {
                    return IsElementPublic((MemberElement)_myIndexerElement);
                }
            }
        }

        public override bool IsStatic => false;
        public override bool IsExtensionMethod => false;
    }
}
