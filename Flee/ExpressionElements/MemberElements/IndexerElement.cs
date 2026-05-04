using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;


namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// Element representing an array index.
    /// </summary>
    internal class IndexerElement(ArgumentList indexer) : MemberElement
    {
        private ExpressionElement _myIndexerElement = null!;

        private readonly ArgumentList _myIndexerElements = indexer;

        protected override void ResolveInternal()
        {
            // Are we are indexing on an array?
            Type target = MyPrevious!.TargetType;

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
            else if (!ImplicitConverter.EmitImplicitConvert(_myIndexerElement.ResultType, typeof(Int32), null))
            {
                ThrowCompileException(CompileErrorResourceKeys.ArrayIndexersMustBeOfType, CompileExceptionReason.TypeMismatch, nameof(Int32));
            }
        }

        private bool FindIndexer(Type targetType)
        {
            // Get the default members
            MemberInfo[] members = targetType.GetDefaultMembers();

            List<MethodInfo> methods = [];

            // Use the first one that's valid for our indexer type
            foreach (MemberInfo mi in members)
            {
                PropertyInfo? pi = mi as PropertyInfo;
                if (pi != null)
                {
                    MethodInfo? getter = pi.GetGetMethod(true);
                    if (getter != null)
                    {
                        methods.Add(getter);
                    }
                }
            }

            FunctionCallElement func = new("Indexer", [.. methods], _myIndexerElements);
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
            _ = ImplicitConverter.EmitImplicitConvert(_myIndexerElement.ResultType, typeof(Int32), ilg);

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

        private Type? ArrayType => IsArray ? MyPrevious!.TargetType : null;

        private bool IsArray => MyPrevious!.TargetType.IsArray;

        protected override bool RequiresAddress => !IsArray;

        public override Type ResultType => IsArray ? ArrayType!.GetElementType()! : _myIndexerElement.ResultType;

        protected override bool IsPublic => IsArray || IsElementPublic((MemberElement)_myIndexerElement);

        public override bool IsStatic => false;
        public override bool IsExtensionMethod => false;
    }
}
