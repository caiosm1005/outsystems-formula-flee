using System.Reflection;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Imports a single static method so it becomes callable from expressions by its short name.
    /// </summary>
    public sealed class MethodImport : ImportBase
    {
        /// <summary>
        /// Initializes a new <see cref="MethodImport"/> wrapping the given method.
        /// </summary>
        /// <param name="importMethod">The method to expose to expressions.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="importMethod"/> is <see langword="null"/>.
        /// </exception>
        public MethodImport(MethodInfo importMethod)
        {
            Utility.AssertNotNull(importMethod, "importMethod");
            Target = importMethod;
        }

        /// <summary>
        /// Verifies the declaring type is accessible to the current expression context.
        /// </summary>
        internal override void Validate()
        {
            Context.AssertTypeIsAccessible(Target.ReflectedType!);
        }

        /// <summary>
        /// Adds the imported method to <paramref name="dest"/> if it matches by name and member type.
        /// </summary>
        /// <param name="memberName">The name to match against.</param>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            bool nameMatches = string.Equals(memberName, Target.Name, Context.Options.MemberStringComparison);
            if (nameMatches && (memberType & MemberTypes.Method) != 0)
            {
                dest.Add(Target);
            }
        }

        /// <summary>
        /// Adds the imported method to <paramref name="dest"/> when methods are requested.
        /// </summary>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if ((memberType & MemberTypes.Method) != 0)
            {
                dest.Add(Target);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="name"/> matches the imported method's name under the
        /// current case-sensitivity policy.
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <returns><see langword="true"/> when the names match.</returns>
        internal override bool IsMatch(string name)
        {
            return string.Equals(Target.Name, name, Context.Options.MemberStringComparison);
        }

        /// <summary>
        /// Method imports do not expose types; always returns <see langword="null"/>.
        /// </summary>
        /// <param name="typeName">Ignored.</param>
        /// <returns>Always <see langword="null"/>.</returns>
        internal override Type? FindType(string typeName)
        {
            return null;
        }

        /// <summary>
        /// Two method imports are equal when they wrap the same underlying method handle.
        /// </summary>
        /// <param name="import">The other import to compare with.</param>
        /// <returns><see langword="true"/> when both imports target the same method.</returns>
        protected override bool EqualsInternal(ImportBase import)
        {
            return (import is MethodImport otherSameType)
                && Target.MethodHandle.Equals(otherSameType.Target.MethodHandle);
        }

        /// <summary>
        /// Gets the imported method's name.
        /// </summary>
        public override string Name => Target.Name;

        /// <summary>
        /// Gets the underlying <see cref="MethodInfo"/> being imported.
        /// </summary>
        public MethodInfo Target { get; }
    }
}
