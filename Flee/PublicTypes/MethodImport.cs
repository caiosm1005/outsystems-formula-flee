using System.Reflection;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    public sealed class MethodImport : ImportBase
    {
        public MethodImport(MethodInfo importMethod)
        {
            Utility.AssertNotNull(importMethod, "importMethod");
            Target = importMethod;
        }

        internal override void Validate()
        {
            Context.AssertTypeIsAccessible(Target.ReflectedType!);
        }

        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (string.Equals(memberName, Target.Name, Context.Options.MemberStringComparison) && (memberType & MemberTypes.Method) != 0)
            {
                dest.Add(Target);
            }
        }

        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if ((memberType & MemberTypes.Method) != 0)
            {
                dest.Add(Target);
            }
        }

        internal override bool IsMatch(string name)
        {
            return string.Equals(Target.Name, name, Context.Options.MemberStringComparison);
        }

        internal override Type? FindType(string typeName)
        {
            return null;
        }

        protected override bool EqualsInternal(ImportBase import)
        {
            return (import is MethodImport otherSameType) && Target.MethodHandle.Equals(otherSameType.Target.MethodHandle);
        }

        public override string Name => Target.Name;

        public MethodInfo Target { get; }
    }
}
