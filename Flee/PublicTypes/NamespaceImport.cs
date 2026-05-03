using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public sealed class NamespaceImport : ImportBase, ICollection<ImportBase>
    {
        private readonly string _myNamespace;
        private readonly List<ImportBase> _myImports;
        public NamespaceImport(string importNamespace)
        {
            Utility.AssertNotNull(importNamespace, "importNamespace");
            if (importNamespace.Length == 0)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.InvalidNamespaceName);
                throw new ArgumentException(msg);
            }

            _myNamespace = importNamespace;
            _myImports = new List<ImportBase>();
        }

        internal override void SetContext(ExpressionContext context)
        {
            base.SetContext(context);

            foreach (ImportBase import in _myImports)
            {
                import.SetContext(context);
            }
        }

        internal override void Validate()
        {
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            foreach (ImportBase import in this.NonContainerImports)
            {
                AddImportMembers(import, memberName, memberType, dest);
            }
        }

        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
        }

        internal override Type? FindType(string typeName)
        {
            foreach (ImportBase import in this.NonContainerImports)
            {
                Type? t = import.FindType(typeName);

                if ((t != null))
                {
                    return t;
                }
            }

            return null;
        }

        internal override ImportBase? FindImport(string name)
        {
            foreach (ImportBase import in _myImports)
            {
                if (import.IsMatch(name) == true)
                {
                    return import;
                }
            }
            return null;
        }

        internal override bool IsMatch(string name)
        {
            return string.Equals(_myNamespace, name, this.Context.Options.MemberStringComparison);
        }

        private ICollection<ImportBase> NonContainerImports
        {
            get
            {
                List<ImportBase> found = new List<ImportBase>();

                foreach (ImportBase import in _myImports)
                {
                    if (import.IsContainer == false)
                    {
                        found.Add(import);
                    }
                }

                return found;
            }
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            NamespaceImport? otherSameType = import as NamespaceImport;
            return (otherSameType != null) && _myNamespace.Equals(otherSameType._myNamespace, this.Context.Options.MemberStringComparison);
        }

        public override bool IsContainer => true;

        public override string Name => _myNamespace;

        #region "ICollection implementation"
        public void Add(ImportBase item)
        {
            Utility.AssertNotNull(item, "item");

            if ((this.Context != null))
            {
                item.SetContext(this.Context);
            }

            _myImports.Add(item);
        }

        public void Clear()
        {
            _myImports.Clear();
        }

        public bool Contains(ImportBase item)
        {
            return _myImports.Contains(item);
        }

        public void CopyTo(ImportBase[] array, int arrayIndex)
        {
            _myImports.CopyTo(array, arrayIndex);
        }

        public bool Remove(ImportBase item)
        {
            return _myImports.Remove(item);
        }

        public override System.Collections.Generic.IEnumerator<ImportBase> GetEnumerator()
        {
            return _myImports.GetEnumerator();
        }

        public int Count => _myImports.Count;

        public bool IsReadOnly => false;

        #endregion
    }
}
