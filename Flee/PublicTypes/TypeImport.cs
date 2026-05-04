using System.Reflection;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    public sealed class TypeImport : ImportBase
    {
        private readonly BindingFlags _myBindFlags;
        private readonly bool _myUseTypeNameAsNamespace;
        public TypeImport(Type importType) : this(importType, false)
        {
        }

        public TypeImport(Type importType, bool useTypeNameAsNamespace) : this(importType, BindingFlags.Public | BindingFlags.Static, useTypeNameAsNamespace)
        {
        }

        #region "Methods - Non Public"
        internal TypeImport(Type t, BindingFlags flags, bool useTypeNameAsNamespace)
        {
            Utility.AssertNotNull(t, "t");
            Target = t;
            _myBindFlags = flags;
            _myUseTypeNameAsNamespace = useTypeNameAsNamespace;
        }

        internal override void Validate()
        {
            Context.AssertTypeIsAccessible(Target);
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            MemberInfo[] members = Target.FindMembers(memberType, _myBindFlags, Context.Options.MemberFilter, memberName);
            AddMemberRange(members, dest);
        }

        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (!_myUseTypeNameAsNamespace)
            {
                MemberInfo[] members = Target.FindMembers(memberType, _myBindFlags, AlwaysMemberFilter, null);
                AddMemberRange(members, dest);
            }
        }

        internal override bool IsMatch(string name)
        {
            return _myUseTypeNameAsNamespace && string.Equals(Target.Name, name, Context.Options.MemberStringComparison);
        }

        internal override Type? FindType(string typeName)
        {
            return string.Equals(typeName, Target.Name, Context.Options.MemberStringComparison) ? Target : null;
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            return (import is TypeImport otherSameType) && ReferenceEquals(Target, otherSameType.Target);
        }
        #endregion

        #region "Methods - Public"
        public override IEnumerator<ImportBase> GetEnumerator()
        {
            if (_myUseTypeNameAsNamespace)
            {
                List<ImportBase> coll = [new TypeImport(Target, false)];
                return coll.GetEnumerator();
            }
            else
            {
                return base.GetEnumerator();
            }
        }
        #endregion

        #region "Properties - Public"
        public override bool IsContainer => _myUseTypeNameAsNamespace;

        public override string Name => Target.Name;

        public Type Target { get; }

        #endregion
    }
}
