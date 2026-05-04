using System.Collections;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// The <c>in</c> operator. Supports two forms: <c>x in (a, b, c)</c> compares the operand
    /// against each list element; <c>x in collection</c> calls <c>Contains</c> (or <c>ContainsKey</c>
    /// for dictionaries) on the target collection.
    /// </summary>
    internal class InElement : ExpressionElement
    {
        /// <summary>The element we will search for.</summary>
        private readonly ExpressionElement MyOperand;

        /// <summary>The list of elements to compare against (when using the list form).</summary>
        private readonly List<ExpressionElement> MyArguments = null!;

        /// <summary>The collection to look in (when using the collection form).</summary>
        private readonly ExpressionElement? MyTargetCollectionElement;

        /// <summary>The CLR collection type chosen for dispatch (when using the collection form).</summary>
        private Type? MyTargetCollectionType;

        /// <summary>
        /// Initializes for the list form: comparing <paramref name="operand"/> against each
        /// element in <paramref name="listElements"/>.
        /// </summary>
        /// <param name="operand">The element to search for.</param>
        /// <param name="listElements">The literal list of elements to compare against.</param>
        public InElement(ExpressionElement operand, IList listElements)
        {
            MyOperand = operand;

            ExpressionElement[] arr = new ExpressionElement[listElements.Count];
            listElements.CopyTo(arr, 0);

            MyArguments = [.. arr];
            ResolveForListSearch();
        }

        /// <summary>
        /// Initializes for the collection form: calling <c>Contains</c> on
        /// <paramref name="targetCollection"/>.
        /// </summary>
        /// <param name="operand">The element to search for.</param>
        /// <param name="targetCollection">The collection to search in.</param>
        public InElement(ExpressionElement operand, ExpressionElement targetCollection)
        {
            MyOperand = operand;
            MyTargetCollectionElement = targetCollection;
            ResolveForCollectionSearch();
        }

        /// <summary>
        /// Validates that the operand can be compared with every list element.
        /// </summary>
        private void ResolveForListSearch()
        {
            CompareElement ce = new();

            // Validate that our operand is comparable to all elements in the list
            foreach (ExpressionElement argumentElement in MyArguments)
            {
                ce.Initialize(MyOperand, argumentElement, LogicalCompareOperation.Equal);
                ce.Validate();
            }
        }

        /// <summary>
        /// Validates the collection form: <see cref="MyTargetCollectionElement"/> must implement
        /// a known collection interface and the operand must be convertible to its element type.
        /// </summary>
        private void ResolveForCollectionSearch()
        {
            // Try to find a collection type
            MyTargetCollectionType = GetTargetCollectionType();

            if (MyTargetCollectionType == null)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.SearchArgIsNotKnownCollectionType,
                    CompileExceptionReason.TypeMismatch,
                    MyTargetCollectionElement!.ResultType.Name);
            }

            // Validate that the operand type is compatible with the collection
            MethodInfo mi = GetCollectionContainsMethod();
            ParameterInfo p1 = mi.GetParameters()[0];

            if (!ImplicitConverter.EmitImplicitConvert(MyOperand.ResultType, p1.ParameterType, null))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.OperandNotConvertibleToCollectionType,
                    CompileExceptionReason.TypeMismatch,
                    MyOperand.ResultType.Name,
                    p1.ParameterType.Name);
            }
        }

        /// <summary>
        /// Inspects <see cref="MyTargetCollectionElement"/>'s type for a recognized collection
        /// interface (<see cref="ICollection{T}"/>, <see cref="IDictionary{TKey,TValue}"/>,
        /// <see cref="IList{T}"/>).
        /// </summary>
        /// <returns>The matched interface type, or <see langword="null"/> when none.</returns>
        private Type? GetTargetCollectionType()
        {
            Type collType = MyTargetCollectionElement!.ResultType;

            // Try to see if the collection is a generic ICollection or IDictionary
            Type[] interfaces = collType.GetInterfaces();

            foreach (Type interfaceType in interfaces)
            {
                if (!interfaceType.IsGenericType)
                {
                    continue;
                }

                Type genericTypeDef = interfaceType.GetGenericTypeDefinition();

                if (ReferenceEquals(genericTypeDef, typeof(ICollection<>))
                    | ReferenceEquals(genericTypeDef, typeof(IDictionary<,>)))
                {
                    return interfaceType;
                }
            }

            // Try to see if it is a regular IList or IDictionary
            if (typeof(IList<>).IsAssignableFrom(collType))
            {
                return typeof(IList<>);
            }
            else if (typeof(IDictionary<,>).IsAssignableFrom(collType))
            {
                return typeof(IDictionary<,>);
            }

            // Not a known collection type
            return null;
        }

        /// <summary>
        /// Dispatches to the list or collection emit based on which constructor was used.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            if (MyTargetCollectionType != null)
            {
                EmitCollectionIn(ilg, services);
            }
            else
            {
                // Do the real emit
                EmitListIn(ilg, services);
            }
        }

        /// <summary>
        /// Emits a call to <c>Contains</c>/<c>ContainsKey</c> on the target collection.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitCollectionIn(FleeILGenerator ilg, IServiceProvider services)
        {
            // Get the contains method
            MethodInfo mi = GetCollectionContainsMethod();
            ParameterInfo p1 = mi.GetParameters()[0];

            // Load the collection
            MyTargetCollectionElement!.Emit(ilg, services);
            // Load the argument
            MyOperand.Emit(ilg, services);
            // Do an implicit convert if necessary
            _ = ImplicitConverter.EmitImplicitConvert(MyOperand.ResultType, p1.ParameterType, ilg);
            // Call the contains method
            ilg.Emit(OpCodes.Callvirt, mi);
        }

        /// <summary>
        /// Resolves the <c>Contains</c>/<c>ContainsKey</c> method on the target collection type.
        /// </summary>
        /// <returns>The method metadata.</returns>
        private MethodInfo GetCollectionContainsMethod()
        {
            string methodName = "Contains";

            if (MyTargetCollectionType!.IsGenericType
                && ReferenceEquals(MyTargetCollectionType.GetGenericTypeDefinition(), typeof(IDictionary<,>)))
            {
                methodName = "ContainsKey";
            }

            return MyTargetCollectionType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;
        }

        /// <summary>
        /// Emits a sequence of equality compares against the literal list, branching to a
        /// "true terminal" on the first match and falling through to push <c>false</c> when
        /// none matches.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        private void EmitListIn(FleeILGenerator ilg, IServiceProvider services)
        {
            CompareElement ce = new();
            Label endLabel = ilg.DefineLabel();
            Label trueTerminal = ilg.DefineLabel();

            // Cache the operand since we will be comparing against it a lot
            LocalBuilder lb = ilg.DeclareLocal(MyOperand.ResultType);
            int targetIndex = lb.LocalIndex;

            MyOperand.Emit(ilg, services);
            Utility.EmitStoreLocal(ilg, targetIndex);

            // Wrap our operand in a local shim
            LocalBasedElement targetShim = new(MyOperand, targetIndex);

            // Emit the compares
            foreach (ExpressionElement argumentElement in MyArguments)
            {
                ce.Initialize(targetShim, argumentElement, LogicalCompareOperation.Equal);
                ce.Emit(ilg, services);

                EmitBranchToTrueTerminal(ilg, trueTerminal);
            }

            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Br_S, endLabel);

            ilg.MarkLabel(trueTerminal);

            ilg.Emit(OpCodes.Ldc_I4_1);

            ilg.MarkLabel(endLabel);
        }

        /// <summary>
        /// Emits a branch-on-true to <paramref name="trueTerminal"/>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="trueTerminal">The branch target.</param>
        private static void EmitBranchToTrueTerminal(FleeILGenerator ilg, Label trueTerminal)
        {
            ilg.EmitBranchTrue(trueTerminal);
        }

        /// <summary>
        /// Always <see cref="bool"/>: <c>in</c> tests for membership.
        /// </summary>
        public override Type ResultType => typeof(bool);
    }
}
