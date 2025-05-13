using System.Reflection;
using Flee.InternalTypes;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public sealed class ExpressionImports
    {

        private static Dictionary<string, Type> OurBuiltinTypeMap = CreateBuiltinTypeMap();
        private NamespaceImport MyRootImport;
        private TypeImport MyOwnerImport;

        private ExpressionContext MyContext;
        internal ExpressionImports()
        {
            MyRootImport = new NamespaceImport("true");
        }

        private static Dictionary<string, Type> CreateBuiltinTypeMap()
        {
            Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
            {
                { "boolean", typeof(bool) },
                { "byte", typeof(byte) },
                { "sbyte", typeof(sbyte) },
                { "short", typeof(short) },
                { "ushort", typeof(ushort) },
                { "int", typeof(int) },
                { "uint", typeof(uint) },
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
            MyRootImport.SetContext(context);
        }

        internal ExpressionImports Clone()
        {
            ExpressionImports copy = new()
            {
                MyRootImport = (NamespaceImport)MyRootImport.Clone(),
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
            return MyRootImport.FindImport(ns) is NamespaceImport import;
        }

        internal NamespaceImport GetImport(string ns)
        {
            if (ns.Length == 0)
            {
                return MyRootImport;
            }

            if (MyRootImport.FindImport(ns) is not NamespaceImport import)
            {
                import = new NamespaceImport(ns);
                MyRootImport.Add(import);
            }

            return import;
        }

        internal MemberInfo[] FindOwnerMembers(string memberName, MemberTypes memberType)
        {
            return MyOwnerImport.FindMembers(memberName, memberType);
        }

        internal Type FindType(string[] typeNameParts)
        {
            string[] namespaces = new string[typeNameParts.Length - 1];
            string typeName = typeNameParts[typeNameParts.Length - 1];

            Array.Copy(typeNameParts, namespaces, namespaces.Length);
            ImportBase currentImport = MyRootImport;

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

        static internal Type GetBuiltinType(string name)
        {
            Type t;
            if (OurBuiltinTypeMap.TryGetValue(name, out t))
            {
                return t;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region "Methods - Public"
        public void AddType(Type t, string ns)
        {
            Utility.AssertNotNull(t, nameof(t));
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
            Utility.AssertNotNull(methodName, nameof(methodName));
            Utility.AssertNotNull(t, nameof(t));
            Utility.AssertNotNull(ns, "namespace");

            MethodInfo mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

            if (mi == null)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.CouldNotFindPublicStaticMethodOnType, methodName, t.Name);
                throw new ArgumentException(msg);
            }

            AddMethod(mi, ns);
        }

        public void AddMethod(MethodInfo mi, string ns)
        {
            Utility.AssertNotNull(mi, nameof(mi));
            Utility.AssertNotNull(ns, "namespace");

            MyContext.AssertTypeIsAccessible(mi.ReflectedType);

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
        public NamespaceImport RootImport => MyRootImport;

        #endregion
    }
}
