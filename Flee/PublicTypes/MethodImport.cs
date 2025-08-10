using System.Reflection;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    public sealed class MethodImport : ImportBase
    {

        private readonly MethodInfo _myMethod;
        public MethodImport(MethodInfo importMethod)
        {
            Utility.AssertNotNull(importMethod, "importMethod");
            _myMethod = importMethod;
        }

        internal override void Validate()
        {
            this.Context.AssertTypeIsAccessible(_myMethod.ReflectedType);
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (string.Equals(memberName, _myMethod.Name, this.Context.Options.MemberStringComparison) == true && (memberType & MemberTypes.Method) != 0)
            {
                dest.Add(_myMethod);
            }
        }

        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if ((memberType & MemberTypes.Method) != 0)
            {
                dest.Add(_myMethod);
            }
        }

        internal override bool IsMatch(string name)
        {
            return string.Equals(_myMethod.Name, name, this.Context.Options.MemberStringComparison);
        }

        internal override Type FindType(string typeName)
        {
            return null;
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            MethodImport otherSameType = import as MethodImport;
            return (otherSameType != null) && _myMethod.MethodHandle.Equals(otherSameType._myMethod.MethodHandle);
        }

        public override string Name => _myMethod.Name;

        public MethodInfo Target => _myMethod;
    }
}
