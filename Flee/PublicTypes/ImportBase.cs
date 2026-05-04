using System.Reflection;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Base class for the various import kinds (<see cref="TypeImport"/>, <see cref="MethodImport"/>,
    /// <see cref="NamespaceImport"/>) that expose CLR members and types to expressions.
    /// </summary>
    public abstract class ImportBase : IEnumerable<ImportBase>, IEquatable<ImportBase>
    {
        /// <summary>
        /// Initializes a new instance. Internal so subclassing is restricted to the assembly.
        /// </summary>
        internal ImportBase()
        {
        }

        #region "Methods - Non Public"

        /// <summary>
        /// Attaches the expression context and triggers per-import validation.
        /// </summary>
        /// <param name="context">The expression context.</param>
        internal virtual void SetContext(ExpressionContext context)
        {
            Context = context;
            Validate();
        }

        /// <summary>
        /// Performs any per-import validation against the attached context (e.g. type accessibility).
        /// </summary>
        internal abstract void Validate();

        /// <summary>
        /// Adds members exposed by this import that match <paramref name="memberName"/>
        /// to <paramref name="dest"/>.
        /// </summary>
        /// <param name="memberName">The member name to match.</param>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected abstract void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest);

        /// <summary>
        /// Adds all members exposed by this import (without filtering by name) to <paramref name="dest"/>.
        /// </summary>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected abstract void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest);

        /// <summary>
        /// Creates a shallow clone of this import.
        /// </summary>
        /// <returns>A new <see cref="ImportBase"/> instance with the same configuration.</returns>
        internal ImportBase Clone()
        {
            return (ImportBase)MemberwiseClone();
        }

        /// <summary>
        /// Helper that calls the protected name-filtered
        /// <see cref="AddMembers(string, MemberTypes, ICollection{MemberInfo})"/>
        /// on another import — needed because it's protected.
        /// </summary>
        /// <param name="import">The import whose members to add.</param>
        /// <param name="memberName">The member name to match.</param>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected static void AddImportMembers(
            ImportBase import,
            string memberName,
            MemberTypes memberType,
            ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberName, memberType, dest);
        }

        /// <summary>
        /// Helper that calls the protected unfiltered <see cref="AddMembers(MemberTypes, ICollection{MemberInfo})"/>
        /// on another import.
        /// </summary>
        /// <param name="import">The import whose members to add.</param>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <param name="dest">The collection that receives matched members.</param>
        protected static void AddImportMembers(ImportBase import, MemberTypes memberType, ICollection<MemberInfo> dest)
        {
            import.AddMembers(memberType, dest);
        }

        /// <summary>
        /// Appends every <see cref="MemberInfo"/> in <paramref name="members"/> to <paramref name="dest"/>.
        /// </summary>
        /// <param name="members">The source members.</param>
        /// <param name="dest">The destination collection.</param>
        protected static void AddMemberRange(ICollection<MemberInfo> members, ICollection<MemberInfo> dest)
        {
            foreach (MemberInfo mi in members)
            {
                dest.Add(mi);
            }
        }

        /// <summary>
        /// A <see cref="MemberFilter"/> that always returns <see langword="true"/>.
        /// </summary>
        protected static readonly MemberFilter AlwaysMemberFilter = (_, _) => true;

        /// <summary>
        /// Returns whether this import's identity matches <paramref name="name"/>
        /// (e.g. namespace name or imported type/method name).
        /// </summary>
        /// <param name="name">The candidate name.</param>
        /// <returns><see langword="true"/> when matching.</returns>
        internal abstract bool IsMatch(string name);

        /// <summary>
        /// Looks up a CLR <see cref="Type"/> exposed by this import by short name.
        /// </summary>
        /// <param name="typename">The type name to resolve.</param>
        /// <returns>The matching type or <see langword="null"/> when not found.</returns>
        internal abstract Type? FindType(string typename);

        /// <summary>
        /// Looks up a child import by name. The default implementation returns <see langword="null"/>;
        /// container imports override this.
        /// </summary>
        /// <param name="name">The child import name.</param>
        /// <returns>The matching child import or <see langword="null"/> when not found.</returns>
        internal virtual ImportBase? FindImport(string name)
        {
            return null;
        }

        /// <summary>
        /// Convenience wrapper around <see cref="AddMembers(string, MemberTypes, ICollection{MemberInfo})"/>
        /// that returns the matched members as an array.
        /// </summary>
        /// <param name="memberName">The member name to match.</param>
        /// <param name="memberType">The set of member kinds the caller is interested in.</param>
        /// <returns>The matched members.</returns>
        internal MemberInfo[] FindMembers(string memberName, MemberTypes memberType)
        {
            List<MemberInfo> found = [];
            AddMembers(memberName, memberType, found);
            return [.. found];
        }
        #endregion

        #region "Methods - Public"

        /// <summary>
        /// Returns all members of <paramref name="memberType"/> exposed by this import.
        /// </summary>
        /// <param name="memberType">The set of member kinds to return.</param>
        /// <returns>The matched members.</returns>
        public MemberInfo[] GetMembers(MemberTypes memberType)
        {
            List<MemberInfo> found = [];
            AddMembers(memberType, found);
            return [.. found];
        }
        #endregion

        #region "IEnumerable Implementation"

        /// <summary>
        /// Returns an enumerator over child imports. The default implementation enumerates
        /// nothing; container imports override this.
        /// </summary>
        /// <returns>An enumerator over child imports.</returns>
        public virtual IEnumerator<ImportBase> GetEnumerator()
        {
            List<ImportBase> coll = [];
            return coll.GetEnumerator();
        }

        /// <summary>
        /// Bridge from the non-generic <see cref="System.Collections.IEnumerable"/> contract
        /// to the generic <see cref="GetEnumerator"/>.
        /// </summary>
        /// <returns>The non-generic enumerator.</returns>
        private System.Collections.IEnumerator GetEnumerator1()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Explicit non-generic <see cref="System.Collections.IEnumerable.GetEnumerator"/> implementation.
        /// </summary>
        /// <returns>The non-generic enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator1();
        }
        #endregion

        #region "IEquatable Implementation"

        /// <summary>
        /// Returns whether two imports are equal under each subclass's comparison rule.
        /// </summary>
        /// <param name="other">The other import to compare with.</param>
        /// <returns><see langword="true"/> when equal.</returns>
        public bool Equals(ImportBase? other)
        {
            return other != null && EqualsInternal(other);
        }

        /// <summary>
        /// Subclass-specific equality logic. Implementations may assume <paramref name="import"/> is non-null.
        /// </summary>
        /// <param name="import">The other import to compare with.</param>
        /// <returns><see langword="true"/> when equal.</returns>
        protected abstract bool EqualsInternal(ImportBase import);
        #endregion

        #region "Properties - Protected"

        /// <summary>
        /// Gets the expression context this import is attached to. Set by <see cref="SetContext"/>.
        /// </summary>
        protected ExpressionContext Context { get; private set; } = null!;

        #endregion

        #region "Properties - Public"

        /// <summary>
        /// Gets the name by which this import is identified in expressions.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this import is a namespace-like container.
        /// Container imports require their name as a prefix to reach their members.
        /// </summary>
        public virtual bool IsContainer => false;

        #endregion
    }
}
