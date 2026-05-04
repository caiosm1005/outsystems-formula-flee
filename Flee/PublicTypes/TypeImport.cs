using System.Reflection;
using Flee.InternalTypes;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Imports a CLR type so its members (and optionally the type itself, used as a namespace)
    /// are callable from expressions.
    /// </summary>
    public sealed class TypeImport : ImportBase
    {
        private readonly BindingFlags _myBindFlags;
        private readonly bool _myUseTypeNameAsNamespace;

        /// <summary>
        /// Imports <paramref name="importType"/> with public+static members and without using
        /// the type name as a namespace.
        /// </summary>
        /// <param name="importType">The CLR type to import.</param>
        public TypeImport(Type importType) : this(importType, false)
        {
        }

        /// <summary>
        /// Imports <paramref name="importType"/> with public+static members.
        /// </summary>
        /// <param name="importType">The CLR type to import.</param>
        /// <param name="useTypeNameAsNamespace">
        /// When <see langword="true"/> the type's name acts as a namespace prefix in expressions.
        /// </param>
        public TypeImport(Type importType, bool useTypeNameAsNamespace)
            : this(importType,
                BindingFlags.Public | BindingFlags.Static,
                useTypeNameAsNamespace)
        {
        }

        #region "Methods - Non Public"

        /// <summary>
        /// Imports a type with explicit binding flags.
        /// </summary>
        /// <param name="t">The CLR type to import.</param>
        /// <param name="flags">The reflection binding flags used for member lookup.</param>
        /// <param name="useTypeNameAsNamespace">
        /// When <see langword="true"/> the type's name acts as a namespace prefix in expressions.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="t"/> is <see langword="null"/>.
        /// </exception>
        internal TypeImport(Type t, BindingFlags flags, bool useTypeNameAsNamespace)
        {
            Utility.AssertNotNull(t, "t");
            Target = t;
            _myBindFlags = flags;
            _myUseTypeNameAsNamespace = useTypeNameAsNamespace;
        }

        /// <summary>
        /// Verifies the imported type is accessible to the current expression context.
        /// </summary>
        internal override void Validate()
        {
            Context.AssertTypeIsAccessible(Target);
        }

        /// <summary>
        /// Adds members of the imported type matching <paramref name="memberName"/> to <paramref name="dest"/>.
        /// </summary>
        /// <param name="memberName">The member name to match.</param>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            MemberInfo[] members = Target.FindMembers(
                memberType,
                _myBindFlags,
                Context.Options.MemberFilter,
                memberName);
            AddMemberRange(members, dest);
        }

        /// <summary>
        /// Adds all members of the imported type to <paramref name="dest"/>, unless the type
        /// is being used as a namespace (in which case its members are reached via the inner import).
        /// </summary>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            if (!_myUseTypeNameAsNamespace)
            {
                MemberInfo[] members = Target.FindMembers(memberType, _myBindFlags, AlwaysMemberFilter, null);
                AddMemberRange(members, dest);
            }
        }

        /// <summary>
        /// When the type is used as a namespace, returns whether <paramref name="name"/> matches
        /// the type's short name.
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <returns><see langword="true"/> when matching as a namespace prefix.</returns>
        internal override bool IsMatch(string name)
        {
            return _myUseTypeNameAsNamespace
                && string.Equals(Target.Name, name, Context.Options.MemberStringComparison);
        }

        /// <summary>
        /// Returns the imported type when <paramref name="typeName"/> matches its short name.
        /// </summary>
        /// <param name="typeName">The candidate name.</param>
        /// <returns>The type, or <see langword="null"/> when the name does not match.</returns>
        internal override Type? FindType(string typeName)
        {
            return string.Equals(typeName, Target.Name, Context.Options.MemberStringComparison)
                ? Target
                : null;
        }

        /// <summary>
        /// Two type imports are equal when they target the same CLR type.
        /// </summary>
        /// <param name="import">The other import to compare with.</param>
        /// <returns><see langword="true"/> when both imports target the same type.</returns>
        protected override bool EqualsInternal(ImportBase import)
        {
            return (import is TypeImport otherSameType) && ReferenceEquals(Target, otherSameType.Target);
        }
        #endregion

        #region "Methods - Public"

        /// <summary>
        /// Returns an enumerator over child imports. When the type is used as a namespace this
        /// yields a single member-import view of the type; otherwise yields nothing.
        /// </summary>
        /// <returns>An enumerator of child imports.</returns>
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

        /// <summary>
        /// Gets a value indicating whether this import behaves as a namespace container
        /// (i.e. the type name must be used as a prefix to reach its members).
        /// </summary>
        public override bool IsContainer => _myUseTypeNameAsNamespace;

        /// <summary>
        /// Gets the imported type's short name.
        /// </summary>
        public override string Name => Target.Name;

        /// <summary>
        /// Gets the underlying CLR <see cref="Type"/> being imported.
        /// </summary>
        public Type Target { get; }

        #endregion
    }
}
