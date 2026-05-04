using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>
    /// The set of types and methods imported into an <see cref="ExpressionContext"/> so that
    /// expressions can reference them by short name. Also tracks the owner-type import for
    /// resolving members of the expression owner.
    /// </summary>
    public sealed class ExpressionImports
    {
        private static readonly Dictionary<string, Type> OurBuiltinTypeMap = CreateBuiltinTypeMap();
        private TypeImport MyOwnerImport = null!;
        private ExpressionContext MyContext = null!;

        /// <summary>
        /// Initializes a new instance with an empty root namespace import.
        /// </summary>
        internal ExpressionImports()
        {
            RootImport = new NamespaceImport("true");
        }

        /// <summary>
        /// Builds the case-insensitive map of language-style aliases (e.g. "int", "string") to CLR types.
        /// </summary>
        /// <returns>The built-in type map.</returns>
        private static Dictionary<string, Type> CreateBuiltinTypeMap()
        {
            Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
            {
                { "boolean", typeof(bool) },
                { "byte", typeof(byte) },
                { "sbyte", typeof(sbyte) },
                { "short", typeof(short) },
                { "ushort", typeof(UInt16) },
                { "int", typeof(Int32) },
                { "uint", typeof(UInt32) },
                { "long", typeof(long) },
                { "ulong", typeof(ulong) },
                { "single", typeof(float) },
                { "double", typeof(double) },
                { "decimal", typeof(decimal) },
                { "char", typeof(char) },
                { "object", typeof(object) },
                { "string", typeof(string) }
            };

            return map;
        }

        #region "Methods - Non public"

        /// <summary>
        /// Attaches the owning expression context. Called by <see cref="ExpressionContext"/>.
        /// </summary>
        /// <param name="context">The owning context.</param>
        internal void SetContext(ExpressionContext context)
        {
            MyContext = context;
            RootImport.SetContext(context);
        }

        /// <summary>
        /// Creates a deep copy of this set of imports.
        /// </summary>
        /// <returns>A new <see cref="ExpressionImports"/> instance.</returns>
        internal ExpressionImports Clone()
        {
            ExpressionImports copy = new()
            {
                RootImport = (NamespaceImport)RootImport.Clone(),
                MyOwnerImport = MyOwnerImport
            };

            return copy;
        }

        /// <summary>
        /// Sets the import that exposes members of the expression owner instance.
        /// </summary>
        /// <param name="ownerType">The runtime type of the owner.</param>
        internal void ImportOwner(Type ownerType)
        {
            BindingFlags flags = BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Instance
                | BindingFlags.Static;
            MyOwnerImport = new TypeImport(ownerType, flags, false);
            MyOwnerImport.SetContext(MyContext);
        }

        /// <summary>
        /// Returns whether a namespace named <paramref name="ns"/> is registered.
        /// </summary>
        /// <param name="ns">The namespace name.</param>
        /// <returns><see langword="true"/> when present.</returns>
        internal bool HasNamespace(string ns)
        {
            return RootImport.FindImport(ns) is NamespaceImport;
        }

        /// <summary>
        /// Returns the namespace import named <paramref name="ns"/>, creating it under the
        /// root if it doesn't already exist. The empty string returns the root itself.
        /// </summary>
        /// <param name="ns">The namespace name.</param>
        /// <returns>The namespace import.</returns>
        internal NamespaceImport GetImport(string ns)
        {
            if (ns.Length == 0)
            {
                return RootImport;
            }


            if (RootImport.FindImport(ns) is not NamespaceImport import)
            {
                import = new NamespaceImport(ns);
                RootImport.Add(import);
            }

            return import;
        }

        /// <summary>
        /// Looks up members on the expression owner.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <param name="memberType">The set of member kinds to return.</param>
        /// <returns>The matched members.</returns>
        internal MemberInfo[] FindOwnerMembers(string memberName, MemberTypes memberType)
        {
            return MyOwnerImport.FindMembers(memberName, memberType);
        }

        /// <summary>
        /// Resolves a dotted type reference (e.g. <c>System.Text.StringBuilder</c>) split into
        /// its parts, descending through namespace imports and finally looking up the type.
        /// </summary>
        /// <param name="typeNameParts">The fully-qualified name split on '.'.</param>
        /// <returns>The matching type, or <see langword="null"/> when not found.</returns>
        internal Type? FindType(string[] typeNameParts)
        {
            string[] namespaces = new string[typeNameParts.Length - 1];
            string typeName = typeNameParts[typeNameParts.Length - 1];

            Array.Copy(typeNameParts, namespaces, namespaces.Length);
            ImportBase? currentImport = RootImport;

            foreach (string ns in namespaces)
            {
                currentImport = currentImport.FindImport(ns);
                if (currentImport == null)
                {
                    break;
                }
            }

            return currentImport?.FindType(typeName);
        }

        /// <summary>
        /// Returns the CLR type associated with a built-in alias such as "int" or "string".
        /// </summary>
        /// <param name="name">The alias name.</param>
        /// <returns>The matching type, or <see langword="null"/> when no alias matches.</returns>
        internal static Type? GetBuiltinType(string name)
        {
            return OurBuiltinTypeMap.TryGetValue(name, out Type? t) ? t : null;
        }
        #endregion

        #region "Methods - Public"

        /// <summary>
        /// Imports <paramref name="t"/> into the namespace <paramref name="ns"/> with public+static
        /// member visibility.
        /// </summary>
        /// <param name="t">The type to import.</param>
        /// <param name="ns">The namespace to import into; the empty string places it at the root.</param>
        /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null"/>.</exception>
        public void AddType(Type t, string ns)
        {
            Utility.AssertNotNull(t, "t");
            Utility.AssertNotNull(ns, "namespace");

            MyContext.AssertTypeIsAccessible(t);

            NamespaceImport import = GetImport(ns);
            import.Add(new TypeImport(t, BindingFlags.Public | BindingFlags.Static, false));
        }

        /// <summary>
        /// Imports <paramref name="t"/> at the root.
        /// </summary>
        /// <param name="t">The type to import.</param>
        public void AddType(Type t)
        {
            AddType(t, string.Empty);
        }

        /// <summary>
        /// Imports a single static method, located by name on <paramref name="t"/>, into the
        /// namespace <paramref name="ns"/>.
        /// </summary>
        /// <param name="methodName">The method name.</param>
        /// <param name="t">The declaring type.</param>
        /// <param name="ns">The target namespace.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when no public static method with that name exists.</exception>
        public void AddMethod(string methodName, Type t, string ns)
        {
            Utility.AssertNotNull(methodName, "methodName");
            Utility.AssertNotNull(t, "t");
            Utility.AssertNotNull(ns, "namespace");

            MethodInfo? mi = t.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

            if (mi == null)
            {
                string msg = Utility.GetGeneralErrorMessage(
                    GeneralErrorResourceKeys.CouldNotFindPublicStaticMethodOnType,
                    methodName,
                    t.Name);
                throw new ArgumentException(msg);
            }

            AddMethod(mi, ns);
        }

        /// <summary>
        /// Imports a specific <see cref="MethodInfo"/> into the namespace <paramref name="ns"/>.
        /// </summary>
        /// <param name="mi">The method to import.</param>
        /// <param name="ns">The target namespace.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="mi"/> is not public+static.</exception>
        public void AddMethod(MethodInfo mi, string ns)
        {
            Utility.AssertNotNull(mi, "mi");
            Utility.AssertNotNull(ns, "namespace");

            MyContext.AssertTypeIsAccessible(mi.ReflectedType!);

            if (!mi.IsStatic | !mi.IsPublic)
            {
                string msg = Utility.GetGeneralErrorMessage(
                    GeneralErrorResourceKeys.OnlyPublicStaticMethodsCanBeImported);
                throw new ArgumentException(msg);
            }

            NamespaceImport import = GetImport(ns);
            import.Add(new MethodImport(mi));
        }

        /// <summary>
        /// Imports each built-in CLR type under its language-style alias (e.g. "int" → <see cref="int"/>),
        /// using the alias as the namespace name.
        /// </summary>
        public void ImportBuiltinTypes()
        {
            foreach (KeyValuePair<string, Type> pair in OurBuiltinTypeMap)
            {
                AddType(pair.Value, pair.Key);
            }
        }
        #endregion

        #region "Properties - Public"

        /// <summary>
        /// Gets the root namespace import. All other imports live under this node.
        /// </summary>
        public NamespaceImport RootImport { get; private set; }

        #endregion
    }
}
