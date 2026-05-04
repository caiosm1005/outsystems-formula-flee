using System.Collections;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// Wraps a chain of <see cref="MemberElement"/>s parsed from a dotted invocation
    /// (e.g. <c>a.b.c(d)</c>). Threads the elements together as a linked list, resolves
    /// namespace prefixes, and at emit time delegates to the tail element.
    /// </summary>
    internal class InvocationListElement : ExpressionElement
    {
        private readonly MemberElement _myTail;

        /// <summary>
        /// Initializes a new instance with the parsed chain.
        /// </summary>
        /// <param name="elements">The parsed member-element chain.</param>
        /// <param name="services">The compile services.</param>
        public InvocationListElement(IList elements, IServiceProvider services)
        {
            HandleFirstElement(elements, services);
            LinkElements(elements);
            Resolve(elements, services);
            _myTail = (MemberElement)elements[elements.Count - 1]!;
        }

        /// <summary>
        /// Arrange elements as a linked list, calling <see cref="MemberElement.Link"/> on
        /// each pair.
        /// </summary>
        /// <param name="elements">The element chain.</param>
        private static void LinkElements(IList elements)
        {
            for (int i = 0; i <= elements.Count - 1; i++)
            {
                MemberElement current = (MemberElement)elements[i]!;
                MemberElement? nextElement = null;
                if (i + 1 < elements.Count)
                {
                    nextElement = (MemberElement)elements[i + 1]!;
                }
                current.Link(nextElement);
            }
        }

        /// <summary>
        /// Normalizes the first element. Non-member expressions become an
        /// <see cref="ExpressionMemberElement"/>; otherwise we strip leading namespace prefixes.
        /// </summary>
        /// <param name="elements">The element chain.</param>
        /// <param name="services">The compile services.</param>
        private void HandleFirstElement(IList elements, IServiceProvider services)
        {
            ExpressionElement first = (ExpressionElement)elements[0]!;

            // If the first element is not a member element, then we assume it is an expression
            // and replace it with the correct member element
            if (first is not MemberElement)
            {
                ExpressionMemberElement actualFirst = new(first);
                elements[0] = actualFirst;
            }
            else
            {
                ResolveNamespaces(elements, services);
            }
        }

        /// <summary>
        /// Walks the leading elements as namespace identifiers, descending through
        /// <see cref="ImportBase.FindImport"/> until we hit a non-namespace element.
        /// </summary>
        /// <param name="elements">The element chain.</param>
        /// <param name="services">The compile services.</param>
        private void ResolveNamespaces(IList elements, IServiceProvider services)
        {
            ExpressionContext context = (ExpressionContext)services.GetService(typeof(ExpressionContext))!;
            ImportBase currentImport = context.Imports.RootImport;

            while (true)
            {
                string? name = GetName(elements);

                if (name == null)
                {
                    break;
                }

                ImportBase? import = currentImport.FindImport(name);

                if (import == null)
                {
                    break;
                }

                currentImport = import;
                elements.RemoveAt(0);

                if (elements.Count > 0)
                {
                    MemberElement newFirst = (MemberElement)elements[0]!;
                    newFirst.SetImport(currentImport);
                }
            }

            if (elements.Count == 0)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.NamespaceCannotBeUsedAsType,
                    CompileExceptionReason.TypeMismatch,
                    currentImport.Name);
            }
        }

        /// <summary>
        /// Returns the name of the first identifier element in <paramref name="elements"/>,
        /// or <see langword="null"/> when the chain is empty or doesn't start with an identifier.
        /// </summary>
        /// <param name="elements">The element chain.</param>
        /// <returns>The name, or <see langword="null"/>.</returns>
        private static string? GetName(IList elements)
        {
            if (elements.Count == 0)
            {
                return null;
            }

            // Is the first member a field/property element?
            IdentifierElement? fpe = elements[0] as IdentifierElement;

            return fpe?.MemberName;
        }

        /// <summary>
        /// Calls <see cref="MemberElement.Resolve"/> on each element.
        /// </summary>
        /// <param name="elements">The element chain.</param>
        /// <param name="services">The compile services.</param>
        private static void Resolve(IList elements, IServiceProvider services)
        {
            foreach (MemberElement element in elements)
            {
                element.Resolve(services);
            }
        }

        /// <summary>
        /// Emits the tail element, which transitively emits every preceding element via the
        /// linked list.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _myTail.Emit(ilg, services);
        }

        /// <summary>
        /// Gets the tail element's result type.
        /// </summary>
        public override Type ResultType => _myTail.ResultType;
    }
}
