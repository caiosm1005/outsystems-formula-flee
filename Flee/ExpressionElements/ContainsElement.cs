using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Base.Literals;
using Flee.ExpressionElements.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Identifies the multi-value variant of the <c>CONTAINS</c> operator.
    /// </summary>
    internal enum ContainsQuantifier
    {
        /// <summary>
        /// No quantifier — single-value lookup (<c>collection CONTAINS value</c>).
        /// </summary>
        None,

        /// <summary>
        /// Any-of quantifier — match when at least one value is present.
        /// </summary>
        Any,

        /// <summary>
        /// All-of quantifier — match only when every value is present.
        /// </summary>
        All
    }

    /// <summary>
    /// The <c>CONTAINS</c> operator family. Supports four shapes:
    /// <list type="bullet">
    /// <item><description><c>collection CONTAINS value</c> — single-value lookup.</description></item>
    /// <item><description><c>collection CONTAINS ANY|ALL (v1, v2, …)</c> — literal value list.</description></item>
    /// <item><description><c>collection CONTAINS ANY|ALL otherCollection</c> — runtime collection RHS.</description></item>
    /// <item><description><c>collection CONTAINS /regex/</c> — regex match against string elements.</description></item>
    /// </list>
    /// All variants emit a call into <see cref="ContainsRuntime"/> after evaluating the
    /// collection (and any list elements / RHS) on the IL stack.
    /// </summary>
    internal class ContainsElement : ExpressionElement
    {
        private static readonly MethodInfo _hasMethod =
            typeof(ContainsRuntime).GetMethod(nameof(ContainsRuntime.Has),
                BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo _anyInMethod =
            typeof(ContainsRuntime).GetMethod(nameof(ContainsRuntime.AnyIn),
                BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo _allInMethod =
            typeof(ContainsRuntime).GetMethod(nameof(ContainsRuntime.AllIn),
                BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo _anyMatchesMethod =
            typeof(ContainsRuntime).GetMethod(nameof(ContainsRuntime.AnyMatches),
                BindingFlags.Public | BindingFlags.Static)!;

        private readonly ExpressionElement _collection;
        private readonly ContainsMode _mode;
        private readonly ContainsQuantifier _quantifier;
        private readonly ExpressionElement? _singleValue;
        private readonly IList? _literalList;
        private readonly ExpressionElement? _otherCollection;
        private readonly RegexLiteralElement? _regex;

        private enum ContainsMode
        {
            SingleValue,
            QuantifiedLiteralList,
            QuantifiedCollection,
            Regex
        }

        private ContainsElement(ExpressionElement collection, ExpressionElement value)
        {
            _collection = collection;
            _singleValue = value;
            _mode = ContainsMode.SingleValue;
            ValidateCollection();
        }

        private ContainsElement(ExpressionElement collection, IList literalList, ContainsQuantifier quantifier)
        {
            _collection = collection;
            _literalList = literalList;
            _quantifier = quantifier;
            _mode = ContainsMode.QuantifiedLiteralList;
            ValidateCollection();
        }

        private ContainsElement(
            ExpressionElement collection,
            ExpressionElement otherCollection,
            ContainsQuantifier quantifier)
        {
            _collection = collection;
            _otherCollection = otherCollection;
            _quantifier = quantifier;
            _mode = ContainsMode.QuantifiedCollection;
            ValidateCollection();
            if (!typeof(IEnumerable).IsAssignableFrom(_otherCollection.ResultType))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.SearchArgIsNotKnownCollectionType,
                    CompileExceptionReason.TypeMismatch,
                    _otherCollection.ResultType.Name);
            }
        }

        private ContainsElement(ExpressionElement collection, RegexLiteralElement regex)
        {
            _collection = collection;
            _regex = regex;
            _mode = ContainsMode.Regex;
            ValidateCollection();
        }

        /// <summary>
        /// Inspects the analyzer-built target shape and constructs the matching
        /// <see cref="ContainsElement"/> overload.
        /// </summary>
        /// <param name="collection">The left-hand collection element.</param>
        /// <param name="target">The right-hand target produced by the analyzer.</param>
        /// <param name="services">The compile services (currently unused, kept for symmetry).</param>
        /// <returns>The configured element.</returns>
        public static ContainsElement Build(ExpressionElement collection, object target, IServiceProvider services)
        {
            _ = services;
            switch (target)
            {
                case QuantifiedTarget qt when qt.Payload is IList list:
                    return new ContainsElement(collection, list, qt.Quantifier);
                case QuantifiedTarget qt:
                    return new ContainsElement(collection, (ExpressionElement)qt.Payload, qt.Quantifier);
                case RegexLiteralElement regex:
                    return new ContainsElement(collection, regex);
                case ExpressionElement value:
                    return new ContainsElement(collection, value);
                default:
                    throw new InvalidOperationException(
                        $"Unsupported CONTAINS target shape: {target.GetType().Name}");
            }
        }

        private void ValidateCollection()
        {
            if (!typeof(IEnumerable).IsAssignableFrom(_collection.ResultType))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.SearchArgIsNotKnownCollectionType,
                    CompileExceptionReason.TypeMismatch,
                    _collection.ResultType.Name);
            }
        }

        /// <summary>
        /// Emits the appropriate <see cref="ContainsRuntime"/> call based on which constructor
        /// was used.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            switch (_mode)
            {
                case ContainsMode.SingleValue:
                    EmitSingleValue(ilg, services);
                    break;
                case ContainsMode.QuantifiedLiteralList:
                    EmitQuantifiedLiteralList(ilg, services);
                    break;
                case ContainsMode.QuantifiedCollection:
                    EmitQuantifiedCollection(ilg, services);
                    break;
                case ContainsMode.Regex:
                    EmitRegex(ilg, services);
                    break;
            }
        }

        private void EmitSingleValue(FleeILGenerator ilg, IServiceProvider services)
        {
            _collection.Emit(ilg, services);
            _singleValue!.Emit(ilg, services);
            BoxIfValueType(_singleValue.ResultType, ilg);
            ilg.Emit(OpCodes.Call, _hasMethod);
        }

        private void EmitQuantifiedLiteralList(FleeILGenerator ilg, IServiceProvider services)
        {
            _collection.Emit(ilg, services);

            LiteralElement.EmitLoad(_literalList!.Count, ilg);
            ilg.Emit(OpCodes.Newarr, typeof(object));

            for (int i = 0; i < _literalList.Count; i++)
            {
                ilg.Emit(OpCodes.Dup);
                LiteralElement.EmitLoad(i, ilg);
                ExpressionElement element = (ExpressionElement)_literalList[i]!;
                element.Emit(ilg, services);
                BoxIfValueType(element.ResultType, ilg);
                ilg.Emit(OpCodes.Stelem_Ref);
            }

            ilg.Emit(OpCodes.Call, _quantifier == ContainsQuantifier.All ? _allInMethod : _anyInMethod);
        }

        private void EmitQuantifiedCollection(FleeILGenerator ilg, IServiceProvider services)
        {
            _collection.Emit(ilg, services);
            _otherCollection!.Emit(ilg, services);
            ilg.Emit(OpCodes.Call, _quantifier == ContainsQuantifier.All ? _allInMethod : _anyInMethod);
        }

        private void EmitRegex(FleeILGenerator ilg, IServiceProvider services)
        {
            _collection.Emit(ilg, services);
            _regex!.Emit(ilg, services);
            ilg.Emit(OpCodes.Call, _anyMatchesMethod);
        }

        private static void BoxIfValueType(Type t, FleeILGenerator ilg)
        {
            if (t.IsValueType)
            {
                ilg.Emit(OpCodes.Box, t);
            }
        }

        /// <summary>
        /// Always <see cref="bool"/>: <c>CONTAINS</c> tests for membership.
        /// </summary>
        public override Type ResultType => typeof(bool);

        /// <summary>
        /// Wrapper used by the analyzer to bundle a <see cref="ContainsQuantifier"/> with the
        /// payload (literal list or runtime collection element) that the quantifier applies to.
        /// </summary>
        internal sealed class QuantifiedTarget
        {
            /// <summary>
            /// Initializes a new instance.
            /// </summary>
            /// <param name="quantifier">The ANY/ALL quantifier.</param>
            /// <param name="payload">An <see cref="IList"/> of literal element values, or an
            /// <see cref="ExpressionElement"/> representing the runtime collection.</param>
            public QuantifiedTarget(ContainsQuantifier quantifier, object payload)
            {
                Quantifier = quantifier;
                Payload = payload;
            }

            /// <summary>
            /// Gets the quantifier kind.
            /// </summary>
            public ContainsQuantifier Quantifier { get; }

            /// <summary>
            /// Gets the bundled payload.
            /// </summary>
            public object Payload { get; }
        }
    }
}
