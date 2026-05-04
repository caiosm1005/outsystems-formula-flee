using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;


namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// Indexing operator (<c>x[i]</c>). Handles native CLR arrays directly and routes
    /// indexer-property dispatch through a synthesized <see cref="FunctionCallElement"/>.
    /// </summary>
    /// <param name="indexer">The argument list inside the brackets.</param>
    internal class IndexerElement(ArgumentList indexer) : MemberElement
    {
        private ExpressionElement _myIndexerElement = null!;

        private readonly ArgumentList _myIndexerElements = indexer;

        /// <summary>
        /// Resolves the indexer: native array or default-member property on the target type.
        /// </summary>
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
                ThrowCompileException(
                    CompileErrorResourceKeys.TypeNotArrayAndHasNoIndexerOfType,
                    CompileExceptionReason.TypeMismatch,
                    target.Name,
                    _myIndexerElements);
            }
        }

        /// <summary>
        /// Validates and stores the index expression for native-array indexing. Multiple
        /// indices aren't supported and the index must be convertible to <see cref="int"/>.
        /// </summary>
        private void SetupArrayIndexer()
        {
            _myIndexerElement = _myIndexerElements[0];

            if (_myIndexerElements.Count > 1)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.MultiArrayIndexNotSupported,
                    CompileExceptionReason.TypeMismatch);
            }
            else if (!ImplicitConverter.EmitImplicitConvert(_myIndexerElement.ResultType, typeof(Int32), null))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.ArrayIndexersMustBeOfType,
                    CompileExceptionReason.TypeMismatch,
                    nameof(Int32));
            }
        }

        /// <summary>
        /// Looks for a default-member property indexer on <paramref name="targetType"/>.
        /// Constructs a synthetic <see cref="FunctionCallElement"/> targeting the indexer's
        /// getter and resolves it.
        /// </summary>
        /// <param name="targetType">The type being indexed.</param>
        /// <returns><see langword="true"/> when the indexer is bound.</returns>
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

        /// <summary>
        /// Dispatches to the array or property-indexer emit.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
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

        /// <summary>
        /// Emits a native-array load: the index, an <c>int</c> conversion, and the right
        /// <c>ldelem</c> variant.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
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

        /// <summary>
        /// Emits a value-type array load. When the next element wants the address, emits
        /// <c>ldelema</c>; otherwise emits the typed <c>ldelem</c>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="elementType">The array element type.</param>
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

        /// <summary>
        /// Emits an indexer-property call by delegating to the synthesized
        /// <see cref="FunctionCallElement"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitIndexer(FleeILGenerator ilg, IServiceProvider services)
        {
            FunctionCallElement func = (FunctionCallElement)_myIndexerElement;
            func.EmitFunctionCall(NextRequiresAddress, ilg, services);
        }

        /// <summary>
        /// Gets the predecessor's array type when applicable, otherwise <see langword="null"/>.
        /// </summary>
        private Type? ArrayType => IsArray ? MyPrevious!.TargetType : null;

        /// <summary>
        /// Gets a value indicating whether the predecessor's type is a CLR array.
        /// </summary>
        private bool IsArray => MyPrevious!.TargetType.IsArray;

        /// <summary>
        /// Indexer-property results are addressable; native-array results are not.
        /// </summary>
        protected override bool RequiresAddress => !IsArray;

        /// <summary>
        /// Gets the element type for arrays or the indexer's return type for properties.
        /// </summary>
        public override Type ResultType =>
            IsArray ? ArrayType!.GetElementType()! : _myIndexerElement.ResultType;

        /// <summary>
        /// Native-array indexers are always public; property-indexer accessibility comes from
        /// the underlying element.
        /// </summary>
        protected override bool IsPublic => IsArray || IsElementPublic((MemberElement)_myIndexerElement);

        /// <summary>
        /// Indexers are always instance-bound.
        /// </summary>
        public override bool IsStatic => false;

        /// <summary>
        /// Never an extension method.
        /// </summary>
        public override bool IsExtensionMethod => false;
    }
}
