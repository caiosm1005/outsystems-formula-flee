using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;

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
            Type[] types = [
            typeof(char),
            typeof(byte),
            typeof(sbyte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(float),
            typeof(double)
        ];
            OurBinaryTypes = types;
            Type[,] table = new Type[types.Length, types.Length];
            OurBinaryResultTable = table;
            FillIdentities(types, table);

            // Fill the table
            AddEntry(typeof(UInt32), typeof(UInt64), typeof(UInt64));
            AddEntry(typeof(Int32), typeof(Int64), typeof(Int64));
            AddEntry(typeof(UInt32), typeof(Int64), typeof(Int64));
            AddEntry(typeof(Int32), typeof(UInt32), typeof(Int64));
            AddEntry(typeof(UInt32), typeof(float), typeof(float));
            AddEntry(typeof(UInt32), typeof(double), typeof(double));
            AddEntry(typeof(Int32), typeof(float), typeof(float));
            AddEntry(typeof(Int32), typeof(double), typeof(double));
            AddEntry(typeof(Int64), typeof(float), typeof(float));
            AddEntry(typeof(Int64), typeof(double), typeof(double));
            AddEntry(typeof(UInt64), typeof(float), typeof(float));
            AddEntry(typeof(UInt64), typeof(double), typeof(double));
            AddEntry(typeof(float), typeof(double), typeof(double));

            // Byte
            AddEntry(typeof(byte), typeof(byte), typeof(Int32));
            AddEntry(typeof(byte), typeof(sbyte), typeof(Int32));
            AddEntry(typeof(byte), typeof(Int16), typeof(Int32));
            AddEntry(typeof(byte), typeof(UInt16), typeof(Int32));
            AddEntry(typeof(byte), typeof(Int32), typeof(Int32));
            AddEntry(typeof(byte), typeof(UInt32), typeof(UInt32));
            AddEntry(typeof(byte), typeof(Int64), typeof(Int64));
            AddEntry(typeof(byte), typeof(UInt64), typeof(UInt64));
            AddEntry(typeof(byte), typeof(float), typeof(float));
            AddEntry(typeof(byte), typeof(double), typeof(double));

            // SByte
            AddEntry(typeof(sbyte), typeof(sbyte), typeof(Int32));
            AddEntry(typeof(sbyte), typeof(Int16), typeof(Int32));
            AddEntry(typeof(sbyte), typeof(UInt16), typeof(Int32));
            AddEntry(typeof(sbyte), typeof(Int32), typeof(Int32));
            AddEntry(typeof(sbyte), typeof(UInt32), typeof(long));
            AddEntry(typeof(sbyte), typeof(Int64), typeof(Int64));
            //invalid -- AddEntry(GetType(SByte), GetType(UInt64), GetType(UInt64))
            AddEntry(typeof(sbyte), typeof(float), typeof(float));
            AddEntry(typeof(sbyte), typeof(double), typeof(double));

            // int16
            AddEntry(typeof(Int16), typeof(Int16), typeof(Int32));
            AddEntry(typeof(Int16), typeof(UInt16), typeof(Int32));
            AddEntry(typeof(Int16), typeof(Int32), typeof(Int32));
            AddEntry(typeof(Int16), typeof(UInt32), typeof(long));
            AddEntry(typeof(Int16), typeof(Int64), typeof(Int64));
            //invalid -- AddEntry(GetType(Int16), GetType(UInt64), GetType(UInt64))
            AddEntry(typeof(Int16), typeof(float), typeof(float));
            AddEntry(typeof(Int16), typeof(double), typeof(double));

            // Uint16
            AddEntry(typeof(UInt16), typeof(UInt16), typeof(Int32));
            AddEntry(typeof(UInt16), typeof(Int16), typeof(Int32));
            AddEntry(typeof(UInt16), typeof(Int32), typeof(Int32));
            AddEntry(typeof(UInt16), typeof(UInt32), typeof(UInt32));
            AddEntry(typeof(UInt16), typeof(Int64), typeof(Int64));
            AddEntry(typeof(UInt16), typeof(UInt64), typeof(UInt64));
            AddEntry(typeof(UInt16), typeof(float), typeof(float));
            AddEntry(typeof(UInt16), typeof(double), typeof(double));

            // Char
            AddEntry(typeof(char), typeof(char), typeof(Int32));
            AddEntry(typeof(char), typeof(UInt16), typeof(UInt16));
            AddEntry(typeof(char), typeof(Int32), typeof(Int32));
            AddEntry(typeof(char), typeof(UInt32), typeof(UInt32));
            AddEntry(typeof(char), typeof(Int64), typeof(Int64));
            AddEntry(typeof(char), typeof(UInt64), typeof(UInt64));
            AddEntry(typeof(char), typeof(float), typeof(float));
            AddEntry(typeof(char), typeof(double), typeof(double));
        }

        private ImplicitConverter()
        {
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

        public static bool EmitImplicitConvert(Type sourceType, Type destType, FleeILGenerator? ilg)
        {
            return ReferenceEquals(sourceType, destType)
                || EmitOverloadedImplicitConvert(sourceType, destType, ilg)
                || ImplicitConvertToReferenceType(sourceType, destType, ilg)
                || ImplicitConvertToValueType(sourceType, destType, ilg);
        }

        private static bool EmitOverloadedImplicitConvert(Type sourceType, Type destType, FleeILGenerator? ilg)
        {
            // Look for an implicit operator on the destination type
            MethodInfo? mi = Utility.GetSimpleOverloadedOperator("Implicit", sourceType, destType);

            if (mi == null)
            {
                // No match
                return false;
            }

            ilg?.Emit(OpCodes.Call, mi);

            return true;
        }

        private static bool ImplicitConvertToReferenceType(Type sourceType, Type destType, FleeILGenerator? ilg)
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

        private static bool ImplicitConvertToValueType(Type sourceType, Type destType, FleeILGenerator? ilg)
        {
            // We only handle value types
            if (!sourceType.IsValueType & !destType.IsValueType)
            {
                return false;
            }

            // No implicit conversion to enum.  Have to do this check here since calling GetTypeCode on an enum will return the typecode
            // of the underlying type which screws us up.
            return !(sourceType.IsEnum | destType.IsEnum) && EmitImplicitNumericConvert(sourceType, destType, ilg);
        }

        /// <summary>
        ///Emit an implicit conversion (if the ilg is not null) and returns a value that determines whether the implicit conversion
        /// succeeded
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="destType"></param>
        /// <param name="ilg"></param>
        /// <returns></returns>
        public static bool EmitImplicitNumericConvert(Type sourceType, Type destType, FleeILGenerator? ilg)
        {
            TypeCode sourceTypeCode = Type.GetTypeCode(sourceType);
            TypeCode destTypeCode = Type.GetTypeCode(destType);

            return destTypeCode == TypeCode.Int16 ? ImplicitConvertToInt16(sourceTypeCode)
                : destTypeCode == TypeCode.UInt16 ? ImplicitConvertToUInt16(sourceTypeCode)
                : destTypeCode == TypeCode.Int32 ? ImplicitConvertToInt32(sourceTypeCode)
                : destTypeCode == TypeCode.UInt32 ? ImplicitConvertToUInt32(sourceTypeCode)
                : destTypeCode == TypeCode.Double ? ImplicitConvertToDouble(sourceTypeCode, ilg)
                : destTypeCode == TypeCode.Single ? ImplicitConvertToSingle(sourceTypeCode, ilg)
                : destTypeCode == TypeCode.Int64 ? ImplicitConvertToInt64(sourceTypeCode, ilg)
                : destTypeCode == TypeCode.UInt64 && ImplicitConvertToUInt64(sourceTypeCode, ilg);
        }


        private static bool ImplicitConvertToInt16(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16;
        }

        private static bool ImplicitConvertToUInt16(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.UInt16;
        }

        private static bool ImplicitConvertToInt32(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32;
        }

        private static bool ImplicitConvertToUInt32(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.UInt32;
        }

        private static bool ImplicitConvertToDouble(TypeCode sourceTypeCode, FleeILGenerator? ilg)
        {
            if (sourceTypeCode is TypeCode.Char or TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.Single or TypeCode.Int64)
            {
                EmitConvert(ilg, OpCodes.Conv_R8);
                return true;
            }
            if (sourceTypeCode is TypeCode.UInt32 or TypeCode.UInt64)
            {
                EmitConvert(ilg, OpCodes.Conv_R_Un);
                EmitConvert(ilg, OpCodes.Conv_R8);
                return true;
            }
            return sourceTypeCode == TypeCode.Double;
        }

        private static bool ImplicitConvertToSingle(TypeCode sourceTypeCode, FleeILGenerator? ilg)
        {
            if (sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.Int64)
            {
                EmitConvert(ilg, OpCodes.Conv_R4);
                return true;
            }
            if (sourceTypeCode is TypeCode.UInt32 or TypeCode.UInt64)
            {
                EmitConvert(ilg, OpCodes.Conv_R_Un);
                EmitConvert(ilg, OpCodes.Conv_R4);
                return true;
            }
            return sourceTypeCode == TypeCode.Single;
        }

        private static bool ImplicitConvertToInt64(TypeCode sourceTypeCode, FleeILGenerator? ilg)
        {
            if (sourceTypeCode is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32)
            {
                EmitConvert(ilg, OpCodes.Conv_I8);
                return true;
            }
            if (sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32)
            {
                EmitConvert(ilg, OpCodes.Conv_U8);
                return true;
            }
            return sourceTypeCode == TypeCode.Int64;
        }

        private static bool ImplicitConvertToUInt64(TypeCode sourceTypeCode, FleeILGenerator? ilg)
        {
            if (sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32)
            {
                EmitConvert(ilg, OpCodes.Conv_U8);
                return true;
            }
            return sourceTypeCode == TypeCode.UInt64;
        }

        private static void EmitConvert(FleeILGenerator? ilg, OpCode convertOpcode)
        {
            ilg?.Emit(convertOpcode);
        }

        /// <summary>
        /// Get the result type for a binary operation
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static Type? GetBinaryResultType(Type t1, Type t2)
        {
            int index1 = GetTypeIndex(t1);
            int index2 = GetTypeIndex(t2);

            return index1 == -1 | index2 == -1 ? null : OurBinaryResultTable[index1, index2];
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

            return tc switch
            {
                TypeCode.Byte => 1,
                TypeCode.SByte => 2,
                TypeCode.Char => 3,
                TypeCode.Int16 => 4,
                TypeCode.UInt16 => 5,
                TypeCode.Int32 => 6,
                TypeCode.UInt32 => 7,
                TypeCode.Int64 => 8,
                TypeCode.UInt64 => 9,
                TypeCode.Single => 10,
                TypeCode.Double or TypeCode.Decimal => 11,
                TypeCode.Boolean => 12,
                TypeCode.DateTime => 13,
                TypeCode.Empty or TypeCode.Object or TypeCode.DBNull or TypeCode.String => -1,
                _ => -1,
            };
        }

        private static int GetReferenceTypeImplicitConvertScore(Type sourceType, Type destType)
        {
            return destType.IsInterface ? 100 : GetInheritanceDistance(sourceType, destType);
        }

        private static int GetInheritanceDistance(Type sourceType, Type destType)
        {
            int count = 0;
            Type? current = sourceType;

            while (!ReferenceEquals(current, destType))
            {
                count += 1;
                current = current?.BaseType;
            }

            return count * 1000;
        }

        private static int GetInverseDistanceToObject(Type t)
        {
            int score = 1000;
            Type? current = t.BaseType;

            while (current != null)
            {
                score -= 100;
                current = current.BaseType;
            }

            return score;
        }
    }
}
