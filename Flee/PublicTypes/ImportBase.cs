using System.Reflection;

namespace Flee.PublicTypes
{
    public abstract class ImportBase : IEnumerable<ImportBase>, IEquatable<ImportBase>
    {
        internal ImportBase()
        {
        }

        #region "Methods - Non Public"
        internal virtual void SetContext(ExpressionContext context)
        {
            Context = context;
            Validate();
        }

        internal abstract void Validate();

        protected abstract void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest);
        protected abstract void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest);

        internal ImportBase Clone()
        {
            return (ImportBase)MemberwiseClone();
        }

        protected static void AddImportMembers(ImportBase import, string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberName, memberType, dest);
        }

        protected static void AddImportMembers(ImportBase import, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberType, dest);
        }

        protected static void AddMemberRange(ICollection<MemberInfo> members, ICollection<MemberInfo> dest)
        {
            foreach (MemberInfo mi in members)
            {
                dest.Add(mi);
            }
        }

        protected static readonly MemberFilter AlwaysMemberFilter = (_, _) => true;

        internal abstract bool IsMatch(string name);
        internal abstract Type? FindType(string typename);

        internal virtual ImportBase? FindImport(string name)
        {
            return null;
        }

        internal MemberInfo[] FindMembers(string memberName, MemberTypes memberType)
        {
            List<MemberInfo> found = [];
            AddMembers(memberName, memberType, found);
            return [.. found];
        }
        #endregion

        #region "Methods - Public"
        public MemberInfo[] GetMembers(MemberTypes memberType)
        {
            List<MemberInfo> found = [];
            AddMembers(memberType, found);
            return [.. found];
        }
        #endregion

        #region "IEnumerable Implementation"
        public virtual IEnumerator<ImportBase> GetEnumerator()
        {
            List<ImportBase> coll = [];
            return coll.GetEnumerator();
        }

        private System.Collections.IEnumerator GetEnumerator1()
        {
            return GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }
        #endregion

        #region "IEquatable Implementation"
        public bool Equals(ImportBase? other)
        {
            return other != null && EqualsInternal(other);
        }

        protected abstract bool EqualsInternal(ImportBase import);
        #endregion

        #region "Properties - Protected"
        protected ExpressionContext Context { get; private set; } = null!;

        #endregion

        #region "Properties - Public"
        public abstract string Name { get; }

        public virtual bool IsContainer => false;

        #endregion
    }
}
