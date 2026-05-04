using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>
    /// A named container of imports. Acts as a namespace from the expression's point of view:
    /// child imports are reachable via the namespace name.
    /// </summary>
    public sealed class NamespaceImport : ImportBase, ICollection<ImportBase>
    {
        private readonly string _myNamespace;
        private readonly List<ImportBase> _myImports;

        /// <summary>
        /// Initializes a new namespace import with the given name.
        /// </summary>
        /// <param name="importNamespace">The namespace name. Must be non-null and non-empty.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="importNamespace"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="importNamespace"/> is empty.</exception>
        public NamespaceImport(string importNamespace)
        {
            Utility.AssertNotNull(importNamespace, "importNamespace");
            if (importNamespace.Length == 0)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.InvalidNamespaceName);
                throw new ArgumentException(msg);
            }

            _myNamespace = importNamespace;
            _myImports = [];
        }

        /// <summary>
        /// Sets the expression context on this namespace and propagates it to all child imports.
        /// </summary>
        /// <param name="context">The expression context.</param>
        internal override void SetContext(ExpressionContext context)
        {
            base.SetContext(context);

            foreach (ImportBase import in _myImports)
            {
                import.SetContext(context);
            }
        }

        /// <summary>
        /// Namespaces have no per-instance validation; children validate themselves on context attach.
        /// </summary>
        internal override void Validate()
        {
        }

        /// <summary>
        /// Visits each non-container child import and forwards member resolution to it.
        /// </summary>
        /// <param name="memberName">The member name to match.</param>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            foreach (ImportBase import in NonContainerImports)
            {
                AddImportMembers(import, memberName, memberType, dest);
            }
        }

        /// <summary>
        /// Namespaces themselves don't expose members directly; this is a no-op.
        /// </summary>
        /// <param name="memberType">Ignored.</param>
        /// <param name="dest">Ignored.</param>
        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
        }

        /// <summary>
        /// Searches non-container child imports for a type matching <paramref name="typeName"/>.
        /// </summary>
        /// <param name="typeName">The type name to match.</param>
        /// <returns>The matching <see cref="Type"/> or <see langword="null"/> when not found.</returns>
        internal override Type? FindType(string typeName)
        {
            foreach (ImportBase import in NonContainerImports)
            {
                Type? t = import.FindType(typeName);

                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a child import by name (any container or leaf).
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <returns>The matching child import or <see langword="null"/> when not found.</returns>
        internal override ImportBase? FindImport(string name)
        {
            foreach (ImportBase import in _myImports)
            {
                if (import.IsMatch(name))
                {
                    return import;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns whether this namespace's name matches <paramref name="name"/> under the
        /// current case-sensitivity policy.
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <returns><see langword="true"/> when the names match.</returns>
        internal override bool IsMatch(string name)
        {
            return string.Equals(_myNamespace, name, Context.Options.MemberStringComparison);
        }

        /// <summary>
        /// Gets the subset of child imports that are not themselves namespace containers.
        /// </summary>
        private ICollection<ImportBase> NonContainerImports
        {
            get
            {
                List<ImportBase> found = [];

                foreach (ImportBase import in _myImports)
                {
                    if (!import.IsContainer)
                    {
                        found.Add(import);
                    }
                }

                return found;
            }
        }

        /// <summary>
        /// Two namespace imports are equal when their names match under the configured comparison.
        /// </summary>
        /// <param name="import">The other import to compare with.</param>
        /// <returns><see langword="true"/> when the namespaces have the same name.</returns>
        protected override bool EqualsInternal(ImportBase import)
        {
            return (import is NamespaceImport otherSameType)
                && _myNamespace.Equals(otherSameType._myNamespace, Context.Options.MemberStringComparison);
        }

        /// <summary>
        /// Always <see langword="true"/>: a namespace is a container.
        /// </summary>
        public override bool IsContainer => true;

        /// <summary>
        /// Gets the namespace name.
        /// </summary>
        public override string Name => _myNamespace;

        #region "ICollection implementation"

        /// <summary>
        /// Adds a child import to this namespace.
        /// </summary>
        /// <param name="item">The import to add.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="item"/> is <see langword="null"/>.
        /// </exception>
        public void Add(ImportBase item)
        {
            Utility.AssertNotNull(item, "item");

            if (Context != null)
            {
                item.SetContext(Context);
            }

            _myImports.Add(item);
        }

        /// <summary>
        /// Removes all child imports.
        /// </summary>
        public void Clear()
        {
            _myImports.Clear();
        }

        /// <summary>
        /// Returns whether <paramref name="item"/> is one of the child imports.
        /// </summary>
        /// <param name="item">The import to look for.</param>
        /// <returns><see langword="true"/> when present.</returns>
        public bool Contains(ImportBase item)
        {
            return _myImports.Contains(item);
        }

        /// <summary>
        /// Copies the child imports to <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The first index to write to.</param>
        public void CopyTo(ImportBase[] array, int arrayIndex)
        {
            _myImports.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes <paramref name="item"/> from the child imports.
        /// </summary>
        /// <param name="item">The import to remove.</param>
        /// <returns><see langword="true"/> when removed; <see langword="false"/> when not present.</returns>
        public bool Remove(ImportBase item)
        {
            return _myImports.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator over the child imports.
        /// </summary>
        /// <returns>An enumerator over child imports.</returns>
        public override IEnumerator<ImportBase> GetEnumerator()
        {
            return _myImports.GetEnumerator();
        }

        /// <summary>
        /// Gets the number of child imports.
        /// </summary>
        public int Count => _myImports.Count;

        /// <summary>
        /// Always <see langword="false"/>: namespaces accept new imports.
        /// </summary>
        public bool IsReadOnly => false;

        #endregion
    }
}
