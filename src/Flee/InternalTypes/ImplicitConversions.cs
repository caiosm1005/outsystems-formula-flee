using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    internal class ImplicitConverter
    {
        /// <summary>
        /// Table of results for binary operations using primitives
        /// </summary>
        private static readonly Type[,] OurBinaryResultTable;

        /// <summary>
        /// Primitive types we support
        /// </summary>
        private static readonly Type[] OurBinaryTypes;
        static ImplicitConverter()
        {
            // Create a table with all the primitive types
            Type[] types = {
                typeof(char),
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double)
            };
            OurBinaryTypes = types;
            Type[,] table = new Type[types.Length, types.Length];
            OurBinaryResultTable = table;
            FillIdentities(types, table);

            // Fill the table
            AddEntry(typeof(uint), typeof(ulong), typeof(ulong));
            AddEntry(typeof(int), typeof(long), typeof(long));
            AddEntry(typeof(uint), typeof(long), typeof(long));
            AddEntry(typeof(int), typeof(uint), typeof(long));
            AddEntry(typeof(uint), typeof(float), typeof(float));
            AddEntry(typeof(uint), typeof(double), typeof(double));
            AddEntry(typeof(int), typeof(float), typeof(float));
            AddEntry(typeof(int), typeof(double), typeof(double));
            AddEntry(typeof(long), typeof(float), typeof(float));
            AddEntry(typeof(long), typeof(double), typeof(double));
            AddEntry(typeof(ulong), typeof(float), typeof(float));
            AddEntry(typeof(ulong), typeof(double), typeof(double));
            AddEntry(typeof(float), typeof(double), typeof(double));

            // Byte
            AddEntry(typeof(byte), typeof(byte), typeof(int));
            AddEntry(typeof(byte), typeof(sbyte), typeof(int));
            AddEntry(typeof(byte), typeof(short), typeof(int));
            AddEntry(typeof(byte), typeof(ushort), typeof(int));
            AddEntry(typeof(byte), typeof(int), typeof(int));
            AddEntry(typeof(byte), typeof(uint), typeof(uint));
            AddEntry(typeof(byte), typeof(long), typeof(long));
            AddEntry(typeof(byte), typeof(ulong), typeof(ulong));
            AddEntry(typeof(byte), typeof(float), typeof(float));
            AddEntry(typeof(byte), typeof(double), typeof(double));

            // SByte
            AddEntry(typeof(sbyte), typeof(sbyte), typeof(int));
            AddEntry(typeof(sbyte), typeof(short), typeof(int));
            AddEntry(typeof(sbyte), typeof(ushort), typeof(int));
            AddEntry(typeof(sbyte), typeof(int), typeof(int));
            AddEntry(typeof(sbyte), typeof(uint), typeof(long));
            AddEntry(typeof(sbyte), typeof(long), typeof(long));
            //invalid -- AddEntry(GetType(SByte), GetType(UInt64), GetType(UInt64))
            AddEntry(typeof(sbyte), typeof(float), typeof(float));
            AddEntry(typeof(sbyte), typeof(double), typeof(double));

            // int16
            AddEntry(typeof(short), typeof(short), typeof(int));
            AddEntry(typeof(short), typeof(ushort), typeof(int));
            AddEntry(typeof(short), typeof(int), typeof(int));
            AddEntry(typeof(short), typeof(uint), typeof(long));
            AddEntry(typeof(short), typeof(long), typeof(long));
            //invalid -- AddEntry(GetType(Int16), GetType(UInt64), GetType(UInt64))
            AddEntry(typeof(short), typeof(float), typeof(float));
            AddEntry(typeof(short), typeof(double), typeof(double));

            // Uint16
            AddEntry(typeof(ushort), typeof(ushort), typeof(int));
            AddEntry(typeof(ushort), typeof(short), typeof(int));
            AddEntry(typeof(ushort), typeof(int), typeof(int));
            AddEntry(typeof(ushort), typeof(uint), typeof(uint));
            AddEntry(typeof(ushort), typeof(long), typeof(long));
            AddEntry(typeof(ushort), typeof(ulong), typeof(ulong));
            AddEntry(typeof(ushort), typeof(float), typeof(float));
            AddEntry(typeof(ushort), typeof(double), typeof(double));

            // Char
            AddEntry(typeof(char), typeof(char), typeof(int));
            AddEntry(typeof(char), typeof(ushort), typeof(ushort));
            AddEntry(typeof(char), typeof(int), typeof(int));
            AddEntry(typeof(char), typeof(uint), typeof(uint));
            AddEntry(typeof(char), typeof(long), typeof(long));
            AddEntry(typeof(char), typeof(ulong), typeof(ulong));
            AddEntry(typeof(char), typeof(float), typeof(float));
            AddEntry(typeof(char), typeof(double), typeof(double));
        }

        private static void FillIdentities(Type[] typeList, Type[,] table)
        {
            for (int i = 0; i <= typeList.Length - 1; i++)
            {
                Type t = typeList[i];
                table[i, i] = t;
            }
        }

        private static void AddEntry(Type t1, Type t2, Type result)
        {
            int index1 = GetTypeIndex(t1);
            int index2 = GetTypeIndex(t2);
            OurBinaryResultTable[index1, index2] = result;
            OurBinaryResultTable[index2, index1] = result;
        }

        private static int GetTypeIndex(Type t)
        {
            return Array.IndexOf(OurBinaryTypes, t);
        }

        public static bool EmitImplicitConvert(Type sourceType, Type destType, FleeILGenerator ilg, IServiceProvider services)
        {
            if (ReferenceEquals(sourceType, destType) || EmitOverloadedImplicitConvert(sourceType, destType, ilg) || ImplicitConvertToReferenceType(sourceType, destType, ilg))
            {
                return true;
            }
            else
            {
                return ImplicitConvertToValueType(sourceType, destType, ilg, services);
            }
        }

        private static bool EmitOverloadedImplicitConvert(Type sourceType, Type destType, FleeILGenerator ilg)
        {
            // Look for an implicit operator on the destination type
            MethodInfo mi = Utility.GetSimpleOverloadedOperator("Implicit", sourceType, destType);

            if (mi == null)
            {
                // No match
                return false;
            }

            ilg?.Emit(OpCodes.Call, mi);

            return true;
        }

        private static bool ImplicitConvertToReferenceType(Type sourceType, Type destType, FleeILGenerator ilg)
        {
            if (destType.IsValueType)
            {
                return false;
            }

            if (ReferenceEquals(sourceType, typeof(Null)))
            {
                // Null is always convertible to a reference type
                return true;
            }

            if (!destType.IsAssignableFrom(sourceType))
            {
                return false;
            }

            if (sourceType.IsValueType)
            {
                ilg?.Emit(OpCodes.Box, sourceType);
            }

            return true;
        }

        private static bool ImplicitConvertToValueType(Type sourceType, Type destType, FleeILGenerator ilg, IServiceProvider services)
        {
            // We only handle value types
            if (!sourceType.IsValueType & !destType.IsValueType)
            {
                return false;
            }

            // No implicit conversion to enum.  Have to do this check here since calling GetTypeCode on an enum will return the typecode
            // of the underlying type which screws us up.
            if (sourceType.IsEnum | destType.IsEnum)
            {
                return false;
            }

            // Handle string converions
            if (destType == typeof(string))
            {
                if (ilg != null)
                {
                    Utility.EmitToString(sourceType, ilg, services);
                }
                return true;
            }

            // Fallback to numeric conversions
            return EmitImplicitNumericConvert(sourceType, destType, ilg, services);
        }

        public static bool EmitImplicitNumericConvert(Type sourceType, Type destType, FleeILGenerator ilg, IServiceProvider services)
        {
            if (Utility.IsCastableNumericType(sourceType) && Utility.IsCastableNumericType(destType))
            {
                if (ilg != null)
                {
                    Utility.EmitNumericCast(sourceType, destType, ilg, services);
                }
                return true;
            }

            // Unsupported conversion
            return false;
        }

        /// <summary>
        /// Get the result type for a binary operation
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static Type GetBinaryResultType(Type t1, Type t2)
        {
            int index1 = GetTypeIndex(t1);
            int index2 = GetTypeIndex(t2);

            if (index1 == -1 | index2 == -1)
            {
                return null;
            }
            else
            {
                return OurBinaryResultTable[index1, index2];
            }
        }

        public static int GetImplicitConvertScore(Type sourceType, Type destType)
        {
            if (ReferenceEquals(sourceType, destType))
            {
                return 0;
            }

            if (ReferenceEquals(sourceType, typeof(Null)))
            {
                return GetInverseDistanceToObject(destType);
            }

            if (Utility.GetSimpleOverloadedOperator("Implicit", sourceType, destType) != null)
            {
                // Implicit operator conversion, score it at 1 so it's just above the minimum
                return 1;
            }

            if (sourceType.IsValueType)
            {
                if (destType.IsValueType)
                {
                    // Value type -> value type
                    int sourceScore = GetValueTypeImplicitConvertScore(sourceType);
                    int destScore = GetValueTypeImplicitConvertScore(destType);

                    return destScore - sourceScore;
                }
                else
                {
                    // Value type -> reference type
                    return GetReferenceTypeImplicitConvertScore(sourceType, destType);
                }
            }
            else
            {
                if (destType.IsValueType)
                {
                    // Reference type -> value type
                    // Reference types can never be implicitly converted to value types
                    Debug.Fail("No implicit conversion from reference type to value type");
                }
                else
                {
                    // Reference type -> reference type
                    return GetReferenceTypeImplicitConvertScore(sourceType, destType);
                }
            }
            return 0;
        }

        private static int GetValueTypeImplicitConvertScore(Type t)
        {
            TypeCode tc = Type.GetTypeCode(t);

            switch (tc)
            {
                case TypeCode.Byte:
                    return 13;
                case TypeCode.SByte:
                    return 12;
                case TypeCode.Char:
                    return 11;
                case TypeCode.Int16:
                    return 10;
                case TypeCode.UInt16:
                    return 9;
                case TypeCode.Int32:
                    return 8;
                case TypeCode.UInt32:
                    return 7;
                case TypeCode.Int64:
                    return 6;
                case TypeCode.UInt64:
                    return 5;
                case TypeCode.Single:
                    return 4;
                case TypeCode.Double:
                    return 3;
                case TypeCode.Decimal:
                    return 3;
                case TypeCode.Boolean:
                    return 2;
                case TypeCode.DateTime:
                    return 1;
                default:
                    Debug.Assert(false, "unknown value type");
                    return 1000;
            }
        }

        private static int GetReferenceTypeImplicitConvertScore(Type sourceType, Type destType)
        {
            if (destType.IsInterface)
            {
                return 100;
            }
            else
            {
                return GetInheritanceDistance(sourceType, destType);
            }
        }

        private static int GetInheritanceDistance(Type sourceType, Type destType)
        {
            int count = 0;
            Type current = sourceType;

            while (current != null && !ReferenceEquals(current, destType))
            {
                count += 1;
                current = current.BaseType;
            }

            return count * 1000;
        }

        private static int GetInverseDistanceToObject(Type t)
        {
            int score = 1000;
            Type current = t.BaseType;

            while (current != null)
            {
                score -= 100;
                current = current.BaseType;
            }

            return score;
        }
    }
}
