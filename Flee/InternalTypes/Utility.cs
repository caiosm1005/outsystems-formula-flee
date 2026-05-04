using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Flee.Resources;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Holds various shared utility methods.
    /// </summary>
    internal class Utility
    {
        private Utility()
        {
        }

        public static void AssertNotNull(object o, string paramName)
        {
            if (o == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void EmitStoreLocal(FleeILGenerator ilg, int index)
        {
            if (index >= 0 & index <= 3)
            {
                switch (index)
                {
                    case 0:
                        ilg.Emit(OpCodes.Stloc_0);
                        break;
                    case 1:
                        ilg.Emit(OpCodes.Stloc_1);
                        break;
                    case 2:
                        ilg.Emit(OpCodes.Stloc_2);
                        break;
                    case 3:
                        ilg.Emit(OpCodes.Stloc_3);
                        break;
                    default:
                        break;
                }
            }
            else if (index < 256)
            {
                ilg.Emit(OpCodes.Stloc_S, Convert.ToByte(index));
            }
            else
            {
                Debug.Assert(index < 65535, "local index too large");
                ilg.Emit(OpCodes.Stloc, unchecked((short)Convert.ToUInt16(index)));
            }
        }

        public static void EmitLoadLocal(FleeILGenerator ilg, int index)
        {
            Debug.Assert(index >= 0, "Invalid index");

            if (index >= 0 & index <= 3)
            {
                switch (index)
                {
                    case 0:
                        ilg.Emit(OpCodes.Ldloc_0);
                        break;
                    case 1:
                        ilg.Emit(OpCodes.Ldloc_1);
                        break;
                    case 2:
                        ilg.Emit(OpCodes.Ldloc_2);
                        break;
                    case 3:
                        ilg.Emit(OpCodes.Ldloc_3);
                        break;
                    default:
                        break;
                }
            }
            else if (index < 256)
            {
                ilg.Emit(OpCodes.Ldloc_S, Convert.ToByte(index));
            }
            else
            {
                Debug.Assert(index < 65535, "local index too large");
                ilg.Emit(OpCodes.Ldloc, unchecked((short)Convert.ToUInt16(index)));
            }
        }

        public static void EmitLoadLocalAddress(FleeILGenerator ilg, int index)
        {
            Debug.Assert(index >= 0, "Invalid index");

            if (index <= byte.MaxValue)
            {
                ilg.Emit(OpCodes.Ldloca_S, Convert.ToByte(index));
            }
            else
            {
                ilg.Emit(OpCodes.Ldloca, index);
            }
        }

        public static void EmitArrayLoad(FleeILGenerator ilg, Type elementType)
        {
            TypeCode tc = Type.GetTypeCode(elementType);

            if (tc == TypeCode.Byte)
            {
                ilg.Emit(OpCodes.Ldelem_U1);
            }
            else if (tc is TypeCode.SByte or TypeCode.Boolean)
            {
                ilg.Emit(OpCodes.Ldelem_I1);
            }
            else if (tc == TypeCode.Int16)
            {
                ilg.Emit(OpCodes.Ldelem_I2);
            }
            else if (tc == TypeCode.UInt16)
            {
                ilg.Emit(OpCodes.Ldelem_U2);
            }
            else if (tc == TypeCode.Int32)
            {
                ilg.Emit(OpCodes.Ldelem_I4);
            }
            else if (tc == TypeCode.UInt32)
            {
                ilg.Emit(OpCodes.Ldelem_U4);
            }
            else if (tc is TypeCode.Int64 or TypeCode.UInt64)
            {
                ilg.Emit(OpCodes.Ldelem_I8);
            }
            else if (tc == TypeCode.Single)
            {
                ilg.Emit(OpCodes.Ldelem_R4);
            }
            else if (tc == TypeCode.Double)
            {
                ilg.Emit(OpCodes.Ldelem_R8);
            }
            else if (tc is TypeCode.Object or TypeCode.String)
            {
                ilg.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                // Must be a non-primitive value type
                ilg.Emit(OpCodes.Ldelema, elementType);
                ilg.Emit(OpCodes.Ldobj, elementType);
            }
        }

        public static void EmitArrayStore(FleeILGenerator ilg, Type elementType)
        {
            TypeCode tc = Type.GetTypeCode(elementType);

            if (tc is TypeCode.Byte or TypeCode.SByte or TypeCode.Boolean)
            {
                ilg.Emit(OpCodes.Stelem_I1);
            }
            else if (tc is TypeCode.Int16 or TypeCode.UInt16)
            {
                ilg.Emit(OpCodes.Stelem_I2);
            }
            else if (tc is TypeCode.Int32 or TypeCode.UInt32)
            {
                ilg.Emit(OpCodes.Stelem_I4);
            }
            else if (tc is TypeCode.Int64 or TypeCode.UInt64)
            {
                ilg.Emit(OpCodes.Stelem_I8);
            }
            else if (tc == TypeCode.Single)
            {
                ilg.Emit(OpCodes.Stelem_R4);
            }
            else if (tc == TypeCode.Double)
            {
                ilg.Emit(OpCodes.Stelem_R8);
            }
            else if (tc is TypeCode.Object or TypeCode.String)
            {
                ilg.Emit(OpCodes.Stelem_Ref);
            }
            else
            {
                // Must be a non-primitive value type
                ilg.Emit(OpCodes.Stelem, elementType);
            }
        }



        public static bool IsIntegralType(Type t)
        {
            return Type.GetTypeCode(t) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;
        }

        public static Type? GetBitwiseOpType(Type leftType, Type rightType)
        {
            return !IsIntegralType(leftType) || !IsIntegralType(rightType) ? null : ImplicitConverter.GetBinaryResultType(leftType, rightType);
        }

        /// <summary>
        /// Find a simple (unary) overloaded operator
        /// </summary>
        /// <param name="name">The name of the operator</param>
        /// <param name="sourceType">The type to convert from</param>
        /// <param name="destType">The type to convert to (can be null if it's not known beforehand)</param>
        /// <returns>The operator's method or null of no match is found</returns>
        public static MethodInfo? GetSimpleOverloadedOperator(string name, Type sourceType, Type? destType)
        {
            Hashtable data = new()
            {
                { "Name", string.Concat("op_", name) },
                { "sourceType", sourceType },
                { "destType", destType }
            };

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            // Look on the source type and its ancestors
            MemberInfo[] members = [];
            Type? sourceWalk = sourceType;
            do
            {
                members = sourceWalk.FindMembers(MemberTypes.Method, flags, SimpleOverloadedOperatorFilter, data);
            } while (members.Length == 0 && (sourceWalk = sourceWalk.BaseType) != null);

            if (members.Length == 0 && destType != null)
            {
                // Look on the dest type and its ancestors
                Type? destWalk = destType;
                do
                {
                    members = destWalk.FindMembers(MemberTypes.Method, flags, SimpleOverloadedOperatorFilter, data);
                } while (members.Length == 0 && (destWalk = destWalk.BaseType) != null);
            }

            Debug.Assert(members.Length < 2, "Multiple overloaded operators found");

            if (members.Length == 0)
            {
                // No match
                return null;
            }
            else
            {
                return (MethodInfo)members[0];
            }
        }

        /// <summary>
        /// Matches simple overloaded operators
        /// </summary>
        /// <param name="member"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool SimpleOverloadedOperatorFilter(MemberInfo member, object? value)
        {
            IDictionary data = (IDictionary)value!;
            MethodInfo method = (MethodInfo)member;

            bool nameMatch = method.IsSpecialName && method.Name.Equals((string)data["Name"]!, StringComparison.OrdinalIgnoreCase);

            if (!nameMatch)
            {
                return false;
            }

            // destination type might not be known
            Type? destType = (Type?)data["destType"];

            if (destType != null)
            {
                bool returnTypeMatch = ReferenceEquals(destType, method.ReturnType);

                if (!returnTypeMatch)
                {
                    return false;
                }
            }

            ParameterInfo[] parameters = method.GetParameters();
            bool argumentMatch = parameters.Length > 0 && parameters[0].ParameterType.IsAssignableFrom((Type)data["sourceType"]!);

            return argumentMatch;
        }

        public static MethodInfo? GetOverloadedOperator(string name, Type sourceType, Binder binder, params Type[] argumentTypes)
        {
            name = string.Concat("op_", name);
            Type? sourceWalk = sourceType;
            do
            {
                MethodInfo? mi = sourceWalk.GetMethod(name, BindingFlags.Public | BindingFlags.Static, binder, CallingConventions.Any, argumentTypes, null);
                if (mi != null && mi.IsSpecialName)
                {
                    return mi;
                }
            } while ((sourceWalk = sourceWalk.BaseType) != null);

            return null;
        }

        public static bool IsLongBranch(int startPosition, int endPosition)
        {
            return (endPosition - startPosition) > sbyte.MaxValue;
        }

        public static string FormatList(string[] items)
        {
            string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";
            return string.Join(separator, items);
        }

        public static string GetGeneralErrorMessage(string key, params object[] args)
        {
            string msg = FleeResourceManager.Instance.GetGeneralErrorString(key) ?? key;
            return string.Format(msg, args);
        }

        public static string GetCompileErrorMessage(string key, params object[] args)
        {
            string msg = FleeResourceManager.Instance.GetCompileErrorString(key) ?? key;
            return string.Format(msg, args);
        }
    }
}
