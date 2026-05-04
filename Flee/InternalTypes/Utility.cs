using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Flee.Resources;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Holds various shared utility methods used across the compiler — argument validation,
    /// IL emit helpers, operator overload lookup, and resource-string formatting.
    /// </summary>
    internal class Utility
    {
        /// <summary>
        /// Private to enforce the static-only utility pattern.
        /// </summary>
        private Utility()
        {
        }

        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="o"/> is <see langword="null"/>.
        /// </summary>
        /// <param name="o">The value to check.</param>
        /// <param name="paramName">The parameter name used in the exception.</param>
        public static void AssertNotNull(object o, string paramName)
        {
            if (o == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Emits the most compact <c>stloc</c> variant for the given local index.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="index">The zero-based local slot index.</param>
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

        /// <summary>
        /// Emits the most compact <c>ldloc</c> variant for the given local index.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="index">The zero-based local slot index.</param>
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

        /// <summary>
        /// Emits a <c>ldloca</c> (or short form) to push the address of the given local.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="index">The zero-based local slot index.</param>
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

        /// <summary>
        /// Emits the appropriate <c>ldelem</c> opcode for the array element type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="elementType">The element CLR type.</param>
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

        /// <summary>
        /// Emits the appropriate <c>stelem</c> opcode for the array element type.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="elementType">The element CLR type.</param>
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

        /// <summary>
        /// Returns whether <paramref name="t"/> is one of the integral CLR primitives.
        /// </summary>
        /// <param name="t">The type to test.</param>
        /// <returns><see langword="true"/> when integral.</returns>
        public static bool IsIntegralType(Type t)
        {
            return Type.GetTypeCode(t) is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                or TypeCode.UInt32
                or TypeCode.Int64
                or TypeCode.UInt64;
        }

        /// <summary>
        /// Returns the result type of a bitwise binary op given two integral operand types,
        /// or <see langword="null"/> when either operand is not integral.
        /// </summary>
        /// <param name="leftType">The left operand type.</param>
        /// <param name="rightType">The right operand type.</param>
        /// <returns>The result type, or <see langword="null"/>.</returns>
        public static Type? GetBitwiseOpType(Type leftType, Type rightType)
        {
            return !IsIntegralType(leftType) || !IsIntegralType(rightType)
                ? null
                : ImplicitConverter.GetBinaryResultType(leftType, rightType);
        }

        /// <summary>
        /// Find a simple (unary) overloaded operator on <paramref name="sourceType"/> or its
        /// ancestors, falling back to <paramref name="destType"/> and its ancestors.
        /// </summary>
        /// <param name="name">The operator name (without the <c>op_</c> prefix).</param>
        /// <param name="sourceType">The type to convert from.</param>
        /// <param name="destType">The type to convert to (may be <see langword="null"/>).</param>
        /// <returns>The operator method, or <see langword="null"/> when no match is found.</returns>
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
        /// <see cref="MemberFilter"/> for <see cref="GetSimpleOverloadedOperator"/>: matches a
        /// special-named method whose first parameter is assignable from the source type and
        /// whose return type matches the destination type when one is supplied.
        /// </summary>
        /// <param name="member">The candidate member.</param>
        /// <param name="value">A hashtable carrying the filter inputs (Name, sourceType, destType).</param>
        /// <returns><see langword="true"/> when the candidate qualifies.</returns>
        private static bool SimpleOverloadedOperatorFilter(MemberInfo member, object? value)
        {
            IDictionary data = (IDictionary)value!;
            MethodInfo method = (MethodInfo)member;

            bool nameMatch = method.IsSpecialName
                && method.Name.Equals((string)data["Name"]!, StringComparison.OrdinalIgnoreCase);

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
            bool argumentMatch = parameters.Length > 0
                && parameters[0].ParameterType.IsAssignableFrom((Type)data["sourceType"]!);

            return argumentMatch;
        }

        /// <summary>
        /// Looks up a public-static overloaded operator whose argument types match
        /// <paramref name="argumentTypes"/>, walking <paramref name="sourceType"/> and its
        /// ancestors and using <paramref name="binder"/> for overload resolution.
        /// </summary>
        /// <param name="name">The operator name (without the <c>op_</c> prefix).</param>
        /// <param name="sourceType">The starting type for the lookup.</param>
        /// <param name="binder">The reflection binder used to choose between overloads.</param>
        /// <param name="argumentTypes">The expected argument types.</param>
        /// <returns>The matching method, or <see langword="null"/> when none is found.</returns>
        public static MethodInfo? GetOverloadedOperator(
            string name,
            Type sourceType,
            Binder binder,
            params Type[] argumentTypes)
        {
            name = string.Concat("op_", name);
            Type? sourceWalk = sourceType;
            do
            {
                MethodInfo? mi = sourceWalk.GetMethod(
                    name,
                    BindingFlags.Public | BindingFlags.Static,
                    binder,
                    CallingConventions.Any,
                    argumentTypes,
                    null);
                if (mi != null && mi.IsSpecialName)
                {
                    return mi;
                }
            } while ((sourceWalk = sourceWalk.BaseType) != null);

            return null;
        }

        /// <summary>
        /// Returns whether a branch from <paramref name="startPosition"/> to
        /// <paramref name="endPosition"/> exceeds the short-branch range.
        /// </summary>
        /// <param name="startPosition">The start IL offset.</param>
        /// <param name="endPosition">The target IL offset.</param>
        /// <returns><see langword="true"/> when long branch is required.</returns>
        public static bool IsLongBranch(int startPosition, int endPosition)
        {
            return (endPosition - startPosition) > sbyte.MaxValue;
        }

        /// <summary>
        /// Joins <paramref name="items"/> using the current culture's list separator
        /// followed by a space (e.g. <c>", "</c>).
        /// </summary>
        /// <param name="items">The strings to join.</param>
        /// <returns>The joined string.</returns>
        public static string FormatList(string[] items)
        {
            string separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";
            return string.Join(separator, items);
        }

        /// <summary>
        /// Returns a formatted general-error message from the resource bundle, falling back to
        /// <paramref name="key"/> when the resource is missing.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="args">The format arguments.</param>
        /// <returns>The formatted message.</returns>
        public static string GetGeneralErrorMessage(string key, params object[] args)
        {
            string msg = FleeResourceManager.Instance.GetGeneralErrorString(key) ?? key;
            return string.Format(msg, args);
        }

        /// <summary>
        /// Returns a formatted compile-error message from the resource bundle, falling back to
        /// <paramref name="key"/> when the resource is missing.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="args">The format arguments.</param>
        /// <returns>The formatted message.</returns>
        public static string GetCompileErrorMessage(string key, params object[] args)
        {
            string msg = FleeResourceManager.Instance.GetCompileErrorString(key) ?? key;
            return string.Format(msg, args);
        }
    }
}
