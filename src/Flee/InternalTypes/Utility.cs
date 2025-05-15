using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Flee.ExpressionElements.Literals;
using Flee.ExpressionElements.Literals.Integral;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Holds various shared utility methods
    /// </summary>
    internal class Utility
    {
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

            switch (tc)
            {
                case TypeCode.Byte:
                    ilg.Emit(OpCodes.Ldelem_U1);
                    break;
                case TypeCode.SByte:
                case TypeCode.Boolean:
                    ilg.Emit(OpCodes.Ldelem_I1);
                    break;
                case TypeCode.Int16:
                    ilg.Emit(OpCodes.Ldelem_I2);
                    break;
                case TypeCode.UInt16:
                    ilg.Emit(OpCodes.Ldelem_U2);
                    break;
                case TypeCode.Int32:
                    ilg.Emit(OpCodes.Ldelem_I4);
                    break;
                case TypeCode.UInt32:
                    ilg.Emit(OpCodes.Ldelem_U4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    ilg.Emit(OpCodes.Ldelem_I8);
                    break;
                case TypeCode.Single:
                    ilg.Emit(OpCodes.Ldelem_R4);
                    break;
                case TypeCode.Double:
                    ilg.Emit(OpCodes.Ldelem_R8);
                    break;
                case TypeCode.Object:
                case TypeCode.String:
                    ilg.Emit(OpCodes.Ldelem_Ref);
                    break;
                default:
                    // Must be a non-primitive value type
                    ilg.Emit(OpCodes.Ldelema, elementType);
                    ilg.Emit(OpCodes.Ldobj, elementType);
                    return;
            }
        }

        public static void EmitArrayStore(FleeILGenerator ilg, Type elementType)
        {
            TypeCode tc = Type.GetTypeCode(elementType);

            switch (tc)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Boolean:
                    ilg.Emit(OpCodes.Stelem_I1);
                    break;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    ilg.Emit(OpCodes.Stelem_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    ilg.Emit(OpCodes.Stelem_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    ilg.Emit(OpCodes.Stelem_I8);
                    break;
                case TypeCode.Single:
                    ilg.Emit(OpCodes.Stelem_R4);
                    break;
                case TypeCode.Double:
                    ilg.Emit(OpCodes.Stelem_R8);
                    break;
                case TypeCode.Object:
                case TypeCode.String:
                    ilg.Emit(OpCodes.Stelem_Ref);
                    break;
                default:
                    // Must be a non-primitive value type
                    ilg.Emit(OpCodes.Stelem, elementType);
                    break;
            }
        }

        public static bool IsIntegralType(Type t)
        {
            TypeCode tc = Type.GetTypeCode(t);
            switch (tc)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public static void EmitToString(Type sourceType, FleeILGenerator ilg, IServiceProvider services)
        {
            TypeCode sourceTypeCode = Type.GetTypeCode(sourceType);

            if (sourceTypeCode == TypeCode.String)
            {
                return; // No conversion needed if source type is string
            }
            else if (sourceTypeCode == TypeCode.Boolean)
            {
                EmitBooleanToString(ilg, services);
            }
            else if (sourceTypeCode == TypeCode.DateTime)
            {
                EmitDateTimeToString(ilg, services);
            }
            else if (sourceTypeCode == TypeCode.Byte ||
                     sourceTypeCode == TypeCode.SByte ||
                     sourceTypeCode == TypeCode.Int16 ||
                     sourceTypeCode == TypeCode.UInt16 ||
                     sourceTypeCode == TypeCode.Int32 ||
                     sourceTypeCode == TypeCode.UInt32 ||
                     sourceTypeCode == TypeCode.Int64 ||
                     sourceTypeCode == TypeCode.UInt64 ||
                     sourceTypeCode == TypeCode.Single ||
                     sourceTypeCode == TypeCode.Double ||
                     sourceTypeCode == TypeCode.Decimal)
            {
                EmitNumberToString(sourceType, ilg, services);
            }
            else
            {
                // For all other types, use the regular ToString() method
                MethodInfo mi = sourceType.GetMethod(nameof(sourceType.ToString), Type.EmptyTypes);
                Debug.Assert(mi != null, "Could not find ToString() method");
                ilg.Emit(OpCodes.Box, sourceType);
                ilg.Emit(OpCodes.Callvirt, mi);
            }
        }

        private static void EmitBooleanToString(FleeILGenerator ilg, IServiceProvider services)
        {
            MethodInfo mi = typeof(FormattingUtils).GetMethod(nameof(FormattingUtils.FormatBoolean), BindingFlags.Static | BindingFlags.Public);
            Debug.Assert(mi != null, "Could not find FormatBoolean() method from Utility class");
            
            // Load parameter (caseSensitive)
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            if (options.CaseSensitive)
            {
                ilg.Emit(OpCodes.Ldc_I4_0); // Pass false to capitalize when case-sensitive
            }
            else
            {
                ilg.Emit(OpCodes.Ldc_I4_1); // Pass true to capitalize when case-insensitive
            }

            // Call FormatBoolean method
            ilg.Emit(OpCodes.Call, mi);
        }

        private static void EmitNumberToString(Type sourceType, FleeILGenerator ilg, IServiceProvider services)
        {
            MethodInfo mi = typeof(FormattingUtils).GetMethod(nameof(FormattingUtils.FormatNumber), BindingFlags.Static | BindingFlags.Public);
            Debug.Assert(mi != null, "Could not find FormatNumber() method from Utility class");

            // Box number value
            ilg.Emit(OpCodes.Box, sourceType);

            // Load parameter (CultureInfo culture)
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            EmitCultureInfo(options.ParseCulture, ilg, services);

            // Call FormatNumber method
            ilg.Emit(OpCodes.Call, mi);
        }

        private static void EmitDateTimeToString(FleeILGenerator ilg, IServiceProvider services)
        {
            MethodInfo mi = typeof(FormattingUtils).GetMethod(nameof(FormattingUtils.FormatDateTime), BindingFlags.Static | BindingFlags.Public);
            Debug.Assert(mi != null, "Could not find FormatDateTime() method from Utility class");

            // Load parameter (string[] formats)
            ExpressionParserOptions parserOptions = (ExpressionParserOptions)services.GetService(typeof(ExpressionParserOptions));
            string[] formats = parserOptions.DateTimeFormats;
            if (formats != null && formats.Length > 0)
            {
                Int32LiteralElement arrayLength = new(formats.Length);
                arrayLength.Emit(ilg, services);
                ilg.Emit(OpCodes.Newarr, typeof(string));
                for (int i = 0; i < formats.Length; i++)
                {
                    ilg.Emit(OpCodes.Dup);
                    Int32LiteralElement arrayIndex = new(i);
                    arrayIndex.Emit(ilg, services);
                    ilg.Emit(OpCodes.Ldstr, formats[i]);
                    ilg.Emit(OpCodes.Stelem_Ref);
                }
            }
            else
            {
                ilg.Emit(OpCodes.Ldnull);
            }

            // Load parameter (CultureInfo culture)
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            EmitCultureInfo(options.ParseCulture, ilg, services);

            // Call FormatDateTime method
            ilg.Emit(OpCodes.Call, mi);
        }

        private static void EmitCultureInfo(CultureInfo culture, FleeILGenerator ilg, IServiceProvider services)
        {
            if (culture.LCID != 0x1000) // Only load culture if it's not unspecified
            {
                Int32LiteralElement cultureId = new(culture.LCID);
                cultureId.Emit(ilg, services);
                ConstructorInfo cultureConstructorInfo = typeof(CultureInfo).GetConstructor(new[] { typeof(int) });
                ilg.Emit(OpCodes.Newobj, cultureConstructorInfo);
            }
            else 
            {
                ilg.Emit(OpCodes.Ldnull);
            }
        }

        public static void EmitNumericCast(Type sourceType, Type destType, FleeILGenerator ilg, IServiceProvider services)
        {
            TypeCode desttc = Type.GetTypeCode(destType);
            TypeCode sourcetc = Type.GetTypeCode(sourceType);
            bool unsigned = IsUnsignedType(sourceType);
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions));
            bool @checked = options.Checked;
            OpCode op = OpCodes.Nop;

            switch (desttc)
            {
                case TypeCode.SByte:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_I1_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_I1;
                    }
                    else
                    {
                        op = OpCodes.Conv_I1;
                    }
                    break;
                case TypeCode.Byte:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_U1_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_U1;
                    }
                    else
                    {
                        op = OpCodes.Conv_U1;
                    }
                    break;
                case TypeCode.Int16:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_I2_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_I2;
                    }
                    else
                    {
                        op = OpCodes.Conv_I2;
                    }
                    break;
                case TypeCode.UInt16:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_U2_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_U2;
                    }
                    else
                    {
                        op = OpCodes.Conv_U2;
                    }
                    break;
                case TypeCode.Int32:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_I4_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_I4;
                    }
                    else if (sourcetc != TypeCode.UInt32)
                    {
                        // Don't need to emit a convert for this case since, to the CLR, it is the same data type
                        op = OpCodes.Conv_I4;
                    }
                    break;
                case TypeCode.UInt32:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_U4_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_U4;
                    }
                    else if (sourcetc != TypeCode.Int32)
                    {
                        op = OpCodes.Conv_U4;
                    }
                    break;
                case TypeCode.Int64:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_I8_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_I8;
                    }
                    else if (sourcetc != TypeCode.UInt64)
                    {
                        op = OpCodes.Conv_I8;
                    }
                    break;
                case TypeCode.UInt64:
                    if (unsigned & @checked)
                    {
                        op = OpCodes.Conv_Ovf_U8_Un;
                    }
                    else if (@checked)
                    {
                        op = OpCodes.Conv_Ovf_U8;
                    }
                    else if (sourcetc != TypeCode.Int64)
                    {
                        op = OpCodes.Conv_U8;
                    }
                    break;
                case TypeCode.Single:
                    op = OpCodes.Conv_R4;
                    break;
                case TypeCode.Double:
                    op = OpCodes.Conv_R8;
                    break;
                default:
                    Debug.Assert(false, "Unknown cast dest type");
                    break;
            }

            if (!op.Equals(OpCodes.Nop))
            {
                ilg.Emit(op);
            }
        }

        public static bool IsCastableNumericType(Type t)
        {
            return t.IsPrimitive && !ReferenceEquals(t, typeof(bool));
        }

        public static bool IsUnsignedType(Type t)
        {
            return Type.GetTypeCode(t) switch
            {
                TypeCode.Byte or
                TypeCode.UInt16 or
                TypeCode.UInt32 or
                TypeCode.UInt64 => true,
                _ => false,
            };
        }

        public static Type GetBitwiseOpType(Type leftType, Type rightType)
        {
            if (!IsIntegralType(leftType) || !IsIntegralType(rightType))
            {
                return null;
            }
            else
            {
                return ImplicitConverter.GetBinaryResultType(leftType, rightType);
            }
        }

        /// <summary>
        /// Find a simple (unary) overloaded operator
        /// </summary>
        /// <param name="name">The name of the operator</param>
        /// <param name="sourceType">The type to convert from</param>
        /// <param name="destType">The type to convert to (can be null if it's not known beforehand)</param>
        /// <returns>The operator's method or null of no match is found</returns>
        public static MethodInfo GetSimpleOverloadedOperator(string name, Type sourceType, Type destType)
        {
            Hashtable data = new()
            {
                { "Name", string.Concat("op_", name) },
                { "sourceType", sourceType },
                { "destType", destType }
            };

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            // Look on the source type and its ancestors
            MemberInfo[] members = new MemberInfo[0];
            do
            {
                members = sourceType.FindMembers(MemberTypes.Method, flags, SimpleOverloadedOperatorFilter, data);
            } while (members.Length == 0 && (sourceType = sourceType.BaseType) != null);

            if (members.Length == 0 && destType != null)
            {
                // Look on the dest type and its ancestors
                do
                {
                    members = destType.FindMembers(MemberTypes.Method, flags, SimpleOverloadedOperatorFilter, data);
                } while (members.Length == 0 && (destType = destType.BaseType) != null);
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
        private static bool SimpleOverloadedOperatorFilter(MemberInfo member, object value)
        {
            IDictionary data = (IDictionary)value;
            MethodInfo method = (MethodInfo)member;

            bool nameMatch = method.IsSpecialName && method.Name.Equals((string)data["Name"], StringComparison.OrdinalIgnoreCase);

            if (!nameMatch)
            {
                return false;
            }

            // destination type might not be known
            Type destType = (Type)data["destType"];

            if (destType != null)
            {
                bool returnTypeMatch = ReferenceEquals(destType, method.ReturnType);

                if (!returnTypeMatch)
                {
                    return false;
                }
            }

            ParameterInfo[] parameters = method.GetParameters();
            bool argumentMatch = parameters.Length > 0 && parameters[0].ParameterType.IsAssignableFrom((Type)data["sourceType"]);

            return argumentMatch;
        }

        public static MethodInfo GetOverloadedOperator(string name, Type sourceType, Binder binder, params Type[] argumentTypes)
        {
            name = string.Concat("op_", name);
            do
            {
                MethodInfo mi = sourceType.GetMethod(name, BindingFlags.Public | BindingFlags.Static, binder, CallingConventions.Any, argumentTypes, null);
                if (mi != null && mi.IsSpecialName)
                {
                    return mi;
                }
            } while ((sourceType = sourceType.BaseType) != null);

            return null;
        }

        public static bool IsLongBranch(int startPosition, int endPosition)
        {
            return (endPosition - startPosition) > sbyte.MaxValue;
        }

        public static string GetGeneralErrorMessage(string key, params object[] args)
        {
            string msg = FleeResourceManager.Instance.GetGeneralErrorString(key);
            return string.Format(msg, args);
        }

        public static string GetCompileErrorMessage(string key, params object[] args)
        {
            string msg = FleeResourceManager.Instance.GetCompileErrorString(key);
            return string.Format(msg, args);
        }
    }
}
