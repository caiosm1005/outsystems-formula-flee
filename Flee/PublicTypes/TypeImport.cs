using System.Reflection;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    public sealed class TypeImport : ImportBase
    {
        private readonly Type _myType;
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
            _myType = t;
            _myBindFlags = flags;
            _myUseTypeNameAsNamespace = useTypeNameAsNamespace;
        }

        internal override void Validate()
        {
            this.Context.AssertTypeIsAccessible(_myType);
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            MemberInfo[] members = _myType.FindMembers(memberType, _myBindFlags, this.Context.Options.MemberFilter, memberName);
            ImportBase.AddMemberRange(members, dest);
        }

        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (_myUseTypeNameAsNamespace == false)
            {
                MemberInfo[] members = _myType.FindMembers(memberType, _myBindFlags, this.AlwaysMemberFilter, null);
                ImportBase.AddMemberRange(members, dest);
            }
        }

        internal override bool IsMatch(string name)
        {
            if (_myUseTypeNameAsNamespace == true)
            {
                return string.Equals(_myType.Name, name, this.Context.Options.MemberStringComparison);
            }
            else
            {
                return false;
            }
        }

        internal override Type FindType(string typeName)
        {
            if (string.Equals(typeName, _myType.Name, this.Context.Options.MemberStringComparison) == true)
            {
                return _myType;
            }
            else
            {
                return null;
            }
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            TypeImport otherSameType = import as TypeImport;
            return (otherSameType != null) && object.ReferenceEquals(_myType, otherSameType._myType);
        }
        #endregion

        #region "Methods - Public"
        public override IEnumerator<ImportBase> GetEnumerator()
        {
            if (_myUseTypeNameAsNamespace == true)
            {
                List<ImportBase> coll = new List<ImportBase>();
                coll.Add(new TypeImport(_myType, false));
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

        public override string Name => _myType.Name;

        public Type Target => _myType;

        #endregion
    }
}
