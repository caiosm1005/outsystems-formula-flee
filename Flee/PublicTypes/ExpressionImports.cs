using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public sealed class ExpressionImports
    {

        private static readonly Dictionary<string, Type> OurBuiltinTypeMap = CreateBuiltinTypeMap();
        private TypeImport MyOwnerImport = null!;

        private ExpressionContext MyContext = null!;
        internal ExpressionImports()
        {
            RootImport = new NamespaceImport("true");
        }

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
        internal void SetContext(ExpressionContext context)
        {
            MyContext = context;
            RootImport.SetContext(context);
        }

        internal ExpressionImports Clone()
        {
            ExpressionImports copy = new()
            {
                RootImport = (NamespaceImport)RootImport.Clone(),
                MyOwnerImport = MyOwnerImport
            };

            return copy;
        }

        internal void ImportOwner(Type ownerType)
        {
            MyOwnerImport = new TypeImport(ownerType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, false);
            MyOwnerImport.SetContext(MyContext);
        }

        internal bool HasNamespace(string ns)
        {
            return RootImport.FindImport(ns) is NamespaceImport;
        }

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

        internal MemberInfo[] FindOwnerMembers(string memberName, MemberTypes memberType)
        {
            return MyOwnerImport.FindMembers(memberName, memberType);
        }

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
                    break; // TODO: might not be correct. Was : Exit For
                }
            }

            return currentImport?.FindType(typeName);
        }

        internal static Type? GetBuiltinType(string name)
        {
            return OurBuiltinTypeMap.TryGetValue(name, out Type? t) ? t : null;
        }
        #endregion

        #region "Methods - Public"
        public void AddType(Type t, string ns)
        {
            Utility.AssertNotNull(t, "t");
            Utility.AssertNotNull(ns, "namespace");

            MyContext.AssertTypeIsAccessible(t);

            NamespaceImport import = GetImport(ns);
            import.Add(new TypeImport(t, BindingFlags.Public | BindingFlags.Static, false));
        }

        public void AddType(Type t)
        {
            AddType(t, string.Empty);
        }

        public void AddMethod(string methodName, Type t, string ns)
        {
            Utility.AssertNotNull(methodName, "methodName");
            Utility.AssertNotNull(t, "t");
            Utility.AssertNotNull(ns, "namespace");

            MethodInfo? mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

            if (mi == null)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.CouldNotFindPublicStaticMethodOnType, methodName, t.Name);
                throw new ArgumentException(msg);
            }

            AddMethod(mi, ns);
        }

        public void AddMethod(MethodInfo mi, string ns)
        {
            Utility.AssertNotNull(mi, "mi");
            Utility.AssertNotNull(ns, "namespace");

            MyContext.AssertTypeIsAccessible(mi.ReflectedType!);

            if (!mi.IsStatic | !mi.IsPublic)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.OnlyPublicStaticMethodsCanBeImported);
                throw new ArgumentException(msg);
            }

            NamespaceImport import = GetImport(ns);
            import.Add(new MethodImport(mi));
        }

        public void ImportBuiltinTypes()
        {
            foreach (KeyValuePair<string, Type> pair in OurBuiltinTypeMap)
            {
                AddType(pair.Value, pair.Key);
            }
        }
        #endregion

        #region "Properties - Public"
        public NamespaceImport RootImport { get; private set; }

        #endregion
    }
}
