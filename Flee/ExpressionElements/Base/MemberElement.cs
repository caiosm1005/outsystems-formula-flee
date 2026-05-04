using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements.Base
{
    /// <summary>
    /// Base class for member-access elements (identifier, function call, indexer, etc.).
    /// Member elements are linked into a left-to-right chain so each can resolve against the
    /// type produced by its predecessor.
    /// </summary>
    internal abstract class MemberElement : ExpressionElement
    {
        /// <summary>
        /// The textual name of this member as it appeared in the source.
        /// </summary>
        protected string MyName = string.Empty;

        /// <summary>
        /// The previous element in the dereference chain, or <see langword="null"/> when first.
        /// </summary>
        protected MemberElement? MyPrevious;

        /// <summary>
        /// The next element in the dereference chain, or <see langword="null"/> when last.
        /// </summary>
        protected MemberElement? MyNext;

        /// <summary>
        /// The compile services container.
        /// </summary>
        protected IServiceProvider MyServices = null!;

        /// <summary>
        /// The compile options.
        /// </summary>
        protected ExpressionOptions MyOptions = null!;

        /// <summary>
        /// The compile context.
        /// </summary>
        protected ExpressionContext MyContext = null!;

        /// <summary>
        /// The import that resolved this element, if any (used by namespaced lookups).
        /// </summary>
        protected ImportBase? MyImport;

        /// <summary>
        /// The reflection binding flags used when looking up members on a type. Encompasses
        /// public + non-public, instance + static.
        /// </summary>
        public const BindingFlags BindFlags = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        protected MemberElement()
        {
        }

        /// <summary>
        /// Links this element to its successor in the dereference chain.
        /// </summary>
        /// <param name="nextElement">The successor element, if any.</param>
        public void Link(MemberElement? nextElement)
        {
            MyNext = nextElement;
            _ = (nextElement?.MyPrevious = this);
        }

        /// <summary>
        /// Captures the per-compile services and runs subclass-specific resolution and validation.
        /// </summary>
        /// <param name="services">The service provider for this compile.</param>
        public void Resolve(IServiceProvider services)
        {
            MyServices = services;
            MyOptions = (ExpressionOptions)services.GetService(typeof(ExpressionOptions))!;
            MyContext = (ExpressionContext)services.GetService(typeof(ExpressionContext))!;
            ResolveInternal();
            Validate();
        }

        /// <summary>
        /// Records the import that resolved this element.
        /// </summary>
        /// <param name="import">The resolving import.</param>
        public void SetImport(ImportBase import)
        {
            MyImport = import;
        }

        /// <summary>
        /// Subclass-specific resolution logic — typically resolving an identifier into a
        /// <see cref="MemberInfo"/> and validating call arguments.
        /// </summary>
        protected abstract void ResolveInternal();

        /// <summary>
        /// Gets a value indicating whether the resolved member is static.
        /// </summary>
        public abstract bool IsStatic { get; }

        /// <summary>
        /// Gets a value indicating whether the resolved member is being invoked as an extension method.
        /// </summary>
        public abstract bool IsExtensionMethod { get; }

        /// <summary>
        /// Gets a value indicating whether the resolved member is publicly accessible.
        /// </summary>
        protected abstract bool IsPublic { get; }

        /// <summary>
        /// Verifies that the static/instance shape of this element is consistent with its
        /// predecessor in the dereference chain.
        /// </summary>
        protected virtual void Validate()
        {
            if (MyPrevious == null)
            {
                return;
            }

            if (IsStatic && !SupportsStatic && !IsExtensionMethod)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.StaticMemberCannotBeAccessedWithInstanceReference,
                    CompileExceptionReason.TypeMismatch,
                    MyName);
            }
            else if (!IsStatic && !SupportsInstance)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.ReferenceToNonSharedMemberRequiresObjectReference,
                    CompileExceptionReason.TypeMismatch,
                    MyName);
            }
        }

        /// <summary>
        /// Default member emit: walks the predecessor first so its result is on the stack
        /// before this element runs. Subclasses override and call <c>base.Emit</c>.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            MyPrevious?.Emit(ilg, services);
        }

        /// <summary>
        /// Emits a load of the variable-collection argument (arg slot 2 of the evaluator delegate).
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        protected static void EmitLoadVariables(FleeILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldarg_2);
        }

        /// <summary>
        /// Handles a call emit for static or instance methods of reference/value types.
        /// </summary>
        /// <param name="mi">The method to call.</param>
        /// <param name="ilg">The IL generator.</param>
        protected void EmitMethodCall(MethodInfo mi, FleeILGenerator ilg)
        {
            EmitMethodCall(ResultType, NextRequiresAddress, mi, ilg);
        }

        /// <summary>
        /// Static helper variant of <see cref="EmitMethodCall(MethodInfo, FleeILGenerator)"/>
        /// that takes the result type and address-required flag explicitly.
        /// </summary>
        /// <param name="resultType">The method's result type.</param>
        /// <param name="nextRequiresAddress">
        /// Whether the next element in the chain expects an address rather than a value.
        /// </param>
        /// <param name="mi">The method to call.</param>
        /// <param name="ilg">The IL generator.</param>
        protected static void EmitMethodCall(
            Type resultType,
            bool nextRequiresAddress,
            MethodInfo mi,
            FleeILGenerator ilg)
        {
            if (!mi.GetType().IsValueType)
            {
                EmitReferenceTypeMethodCall(mi, ilg);
            }
            else
            {
                EmitValueTypeMethodCall(mi, ilg);
            }

            if (resultType.IsValueType & nextRequiresAddress)
            {
                EmitValueTypeLoadAddress(ilg, resultType);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="mi"/> is the <see cref="object.GetType"/> method,
        /// which needs special boxing handling on value types.
        /// </summary>
        /// <param name="mi">The method to inspect.</param>
        /// <returns><see langword="true"/> when this is <c>object.GetType()</c>.</returns>
        protected static bool IsGetTypeMethod(MethodInfo mi)
        {
            MethodInfo? miGetType = typeof(object).GetMethod(
                "gettype",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            return mi.MethodHandle.Equals(miGetType!.MethodHandle);
        }

        /// <summary>
        /// Emit a function call for a value type. Handles static, base-class, and direct dispatch
        /// distinctly because each requires a different opcode pattern.
        /// </summary>
        /// <param name="mi">The method to call.</param>
        /// <param name="ilg">The IL generator.</param>
        private static void EmitValueTypeMethodCall(MethodInfo mi, FleeILGenerator ilg)
        {
            if (mi.IsStatic)
            {
                ilg.Emit(OpCodes.Call, mi);
            }
            else if (!ReferenceEquals(mi.DeclaringType, mi.ReflectedType))
            {
                // Method is not defined on the value type

                if (IsGetTypeMethod(mi))
                {
                    // Special GetType method which requires a box
                    ilg.Emit(OpCodes.Box, mi.ReflectedType!);
                    ilg.Emit(OpCodes.Call, mi);
                }
                else
                {
                    // Equals, GetHashCode, and ToString methods on the base
                    ilg.Emit(OpCodes.Constrained, mi.ReflectedType!);
                    ilg.Emit(OpCodes.Callvirt, mi);
                }
            }
            else
            {
                // Call value type's implementation
                ilg.Emit(OpCodes.Call, mi);
            }
        }

        /// <summary>
        /// Emits a call to a method on a reference type, picking <c>call</c> for static methods
        /// and <c>callvirt</c> for instance methods.
        /// </summary>
        /// <param name="mi">The method to call.</param>
        /// <param name="ilg">The IL generator.</param>
        private static void EmitReferenceTypeMethodCall(MethodInfo mi, FleeILGenerator ilg)
        {
            if (mi.IsStatic)
            {
                ilg.Emit(OpCodes.Call, mi);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, mi);
            }
        }

        /// <summary>
        /// Stores the value on the stack into a temp local of <paramref name="targetType"/>
        /// and pushes the local's address.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="targetType">The value type whose address is needed.</param>
        protected static void EmitValueTypeLoadAddress(FleeILGenerator ilg, Type targetType)
        {
            int index = ilg.GetTempLocalIndex(targetType);
            Utility.EmitStoreLocal(ilg, index);
            ilg.Emit(OpCodes.Ldloca_S, Convert.ToByte(index));
        }

        /// <summary>
        /// Emits a load of the expression-owner argument (arg slot 0). Unboxes value-type owners
        /// and re-loads the address when downstream emit needs it.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        protected void EmitLoadOwner(FleeILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldarg_0);

            Type ownerType = MyOptions.OwnerType;

            if (!ownerType.IsValueType)
            {
                return;
            }

            ilg.Emit(OpCodes.Unbox, ownerType);
            ilg.Emit(OpCodes.Ldobj, ownerType);

            // Emit usual stuff for value types but use the owner type as the target
            if (RequiresAddress)
            {
                EmitValueTypeLoadAddress(ilg, ownerType);
            }
        }

        /// <summary>
        /// Determine if a field, property, or method is public.
        /// </summary>
        /// <param name="member">The member to inspect.</param>
        /// <returns><see langword="true"/> when public.</returns>
        private static bool IsMemberPublic(MemberInfo member)
        {
            FieldInfo? fi = member as FieldInfo;

            if (fi != null)
            {
                return fi.IsPublic;
            }

            PropertyInfo? pi = member as PropertyInfo;

            if (pi != null)
            {
                MethodInfo? pmi = pi.GetGetMethod(true);
                return pmi != null && pmi.IsPublic;
            }

            MethodInfo? mi = member as MethodInfo;

            if (mi != null)
            {
                return mi.IsPublic;
            }

            Debug.Assert(false, "unknown member type");
            return false;
        }

        /// <summary>
        /// Filters <paramref name="members"/> down to those visible to the current expression
        /// under the configured access policy.
        /// </summary>
        /// <param name="members">The candidate members.</param>
        /// <returns>The accessible subset.</returns>
        protected MemberInfo[] GetAccessibleMembers(MemberInfo[] members)
        {
            List<MemberInfo> accessible = [];

            // Keep all members that are accessible
            foreach (MemberInfo mi in members)
            {
                if (IsMemberAccessible(mi))
                {
                    accessible.Add(mi);
                }
            }

            return [.. accessible];
        }

        /// <summary>
        /// Returns whether <paramref name="member"/> on the expression owner is accessible
        /// under <paramref name="options"/>. Honors any
        /// <see cref="ExpressionOwnerMemberAccessAttribute"/> override on the member.
        /// </summary>
        /// <param name="member">The candidate member.</param>
        /// <param name="options">The expression options.</param>
        /// <returns><see langword="true"/> when accessible.</returns>
        protected static bool IsOwnerMemberAccessible(MemberInfo member, ExpressionOptions options)
        {
            bool accessAllowed = IsMemberPublic(member)
                ? (options.OwnerMemberAccess & BindingFlags.Public) != 0
                : (options.OwnerMemberAccess & BindingFlags.NonPublic) != 0;

            // Get the allowed access defined in the options

            // See if the member has our access attribute defined
            ExpressionOwnerMemberAccessAttribute? attr =
                (ExpressionOwnerMemberAccessAttribute?)Attribute.GetCustomAttribute(
                    member,
                    typeof(ExpressionOwnerMemberAccessAttribute));

            if (attr == null)
            {
                // No, so return the access level
                return accessAllowed;
            }
            else
            {
                // Member has our access attribute defined; use its access value instead
                return attr.AllowAccess;
            }
        }

        /// <summary>
        /// Returns whether <paramref name="member"/> is accessible from the current expression.
        /// Members on the expression owner go through the owner-access policy; everything else
        /// must be public.
        /// </summary>
        /// <param name="member">The candidate member.</param>
        /// <returns><see langword="true"/> when accessible.</returns>
        public bool IsMemberAccessible(MemberInfo member)
        {
            return member.ReflectedType != null && MyOptions.IsOwnerType(member.ReflectedType)
                ? IsOwnerMemberAccessible(member, MyOptions)
                : IsMemberPublic(member);
        }

        /// <summary>
        /// Looks up members by name. Without a predecessor we search the default namespace
        /// (or the bound import); otherwise we resolve against the predecessor's result type.
        /// </summary>
        /// <param name="targets">The set of member kinds to return.</param>
        /// <returns>The matched members.</returns>
        protected MemberInfo[] GetMembers(MemberTypes targets)
        {
            if (MyPrevious == null)
            {
                // Do we have a namespace?
                if (MyImport == null)
                {
                    // Get all members in the default namespace
                    return GetDefaultNamespaceMembers(MyName, targets);
                }
                else
                {
                    return MyImport.FindMembers(MyName, targets);
                }
            }
            else
            {
                // We are not the first element; find all members with our name on the type of the previous member
                var foundMembers = MyPrevious.TargetType.FindMembers(
                    targets,
                    BindFlags,
                    MyOptions.MemberFilter,
                    MyName);
                var importedMembers = MyContext.Imports.RootImport.FindMembers(MyName, targets);
                if (foundMembers.Length == 0) //If no members found search in root import
                {
                    return importedMembers;
                }

                MemberInfo[] allMembers = new MemberInfo[foundMembers.Length + importedMembers.Length];
                foundMembers.CopyTo(allMembers, 0);
                importedMembers.CopyTo(allMembers, foundMembers.Length);
                return allMembers;
            }
        }

        /// <summary>
        /// Find members in the default namespace. Searches the expression owner first, filters
        /// by access policy, then merges in any matches from the root import.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="memberType">The set of member kinds to return.</param>
        /// <returns>The matched members.</returns>
        protected MemberInfo[] GetDefaultNamespaceMembers(string name, MemberTypes memberType)
        {
            // Search the owner first
            MemberInfo[] members = MyContext.Imports.FindOwnerMembers(name, memberType);

            // Keep only the accessible members
            members = GetAccessibleMembers(members);

            //Also search imports
            var importedMembers = MyContext.Imports.RootImport.FindMembers(name, memberType);

            //if no members, just return imports
            if (members.Length == 0)
            {
                return importedMembers;
            }

            //combine members and imports
            MemberInfo[] allMembers = new MemberInfo[members.Length + importedMembers.Length];
            members.CopyTo(allMembers, 0);
            importedMembers.CopyTo(allMembers, members.Length);
            return allMembers;
        }

        /// <summary>
        /// Returns whether <paramref name="e"/>'s resolved member is publicly accessible.
        /// </summary>
        /// <param name="e">The candidate element.</param>
        /// <returns><see langword="true"/> when public.</returns>
        protected static bool IsElementPublic(MemberElement e)
        {
            return e.IsPublic;
        }

        /// <summary>
        /// Gets the textual member name from the source.
        /// </summary>
        public string MemberName => MyName;

        /// <summary>
        /// Gets a value indicating whether the next element in the chain requires the address
        /// (rather than the value) of this element's result.
        /// </summary>
        protected bool NextRequiresAddress => MyNext != null && MyNext.RequiresAddress;

        /// <summary>
        /// Gets a value indicating whether this element produces an addressable value.
        /// Subclasses override.
        /// </summary>
        protected virtual bool RequiresAddress => false;

        /// <summary>
        /// Gets a value indicating whether this element supports being applied to an instance.
        /// </summary>
        protected virtual bool SupportsInstance => true;

        /// <summary>
        /// Gets a value indicating whether this element supports being applied statically.
        /// </summary>
        protected virtual bool SupportsStatic => false;

        /// <summary>
        /// Gets the type the next element will dereference against (defaults to
        /// <see cref="ExpressionElement.ResultType"/>).
        /// </summary>
        public Type TargetType => ResultType;
    }
}
