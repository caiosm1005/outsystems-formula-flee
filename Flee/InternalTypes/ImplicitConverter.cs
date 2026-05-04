using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Centralizes the rules and IL emit for implicit conversions between primitive types and
    /// reference-type subtype relationships. Also computes the binary-result type for primitive
    /// arithmetic and the score used by overload resolution.
    /// </summary>
    internal class ImplicitConverter
    {
        /// <summary>
        /// Table of results for binary operations using primitives.
        /// </summary>
        private static readonly Type[,] OurBinaryResultTable;

        /// <summary>
        /// Primitive types we support.
        /// </summary>
        private static readonly Type[] OurBinaryTypes;

        /// <summary>
        /// Builds the lookup table that maps each pair of primitive operand types to the
        /// CLR type the binary operation produces.
        /// </summary>
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

        /// <summary>
        /// Private to enforce the static-only utility pattern.
        /// </summary>
        private ImplicitConverter()
        {
        }

        /// <summary>
        /// Seeds the binary-result table's diagonal so each primitive paired with itself
        /// resolves to itself.
        /// </summary>
        /// <param name="typeList">The primitives, in table order.</param>
        /// <param name="table">The table to populate.</param>
        private static void FillIdentities(Type[] typeList, Type[,] table)
        {
            for (int i = 0; i <= typeList.Length - 1; i++)
            {
                Type t = typeList[i];
                table[i, i] = t;
            }
        }

        /// <summary>
        /// Records that the binary result of <paramref name="t1"/> with <paramref name="t2"/>
        /// (in either order) is <paramref name="result"/>.
        /// </summary>
        /// <param name="t1">First operand type.</param>
        /// <param name="t2">Second operand type.</param>
        /// <param name="result">Result type.</param>
        private static void AddEntry(Type t1, Type t2, Type result)
        {
            int index1 = GetTypeIndex(t1);
            int index2 = GetTypeIndex(t2);
            OurBinaryResultTable[index1, index2] = result;
            OurBinaryResultTable[index2, index1] = result;
        }

        /// <summary>
        /// Returns the row/column index of <paramref name="t"/> in the binary-result table.
        /// </summary>
        /// <param name="t">The type to look up.</param>
        /// <returns>The index, or <c>-1</c> when not a tracked primitive.</returns>
        private static int GetTypeIndex(Type t)
        {
            return Array.IndexOf(OurBinaryTypes, t);
        }

        /// <summary>
        /// Attempts to emit (or merely test, when <paramref name="ilg"/> is <see langword="null"/>)
        /// an implicit conversion from <paramref name="sourceType"/> to <paramref name="destType"/>.
        /// </summary>
        /// <param name="sourceType">The source CLR type.</param>
        /// <param name="destType">The target CLR type.</param>
        /// <param name="ilg">The IL generator, or <see langword="null"/> to test feasibility only.</param>
        /// <returns><see langword="true"/> when the conversion is valid.</returns>
        public static bool EmitImplicitConvert(Type sourceType, Type destType, FleeILGenerator? ilg)
        {
            return ReferenceEquals(sourceType, destType)
                || EmitOverloadedImplicitConvert(sourceType, destType, ilg)
                || ImplicitConvertToReferenceType(sourceType, destType, ilg)
                || ImplicitConvertToValueType(sourceType, destType, ilg);
        }

        /// <summary>
        /// Looks for a user-defined <c>op_Implicit</c> on <paramref name="sourceType"/> or
        /// <paramref name="destType"/> and emits the call when found.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The target type.</param>
        /// <param name="ilg">The IL generator (optional).</param>
        /// <returns><see langword="true"/> when an operator was found.</returns>
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

        /// <summary>
        /// Handles conversions to a reference type: <see cref="Null"/> always succeeds; otherwise
        /// the source type must be assignable to the destination type, and value types are boxed.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The target reference type.</param>
        /// <param name="ilg">The IL generator (optional).</param>
        /// <returns><see langword="true"/> when convertible.</returns>
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

        /// <summary>
        /// Handles conversions to a value type. Enums never participate, and the rest is delegated
        /// to <see cref="EmitImplicitNumericConvert"/>.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The target value type.</param>
        /// <param name="ilg">The IL generator (optional).</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToValueType(Type sourceType, Type destType, FleeILGenerator? ilg)
        {
            // We only handle value types
            if (!sourceType.IsValueType & !destType.IsValueType)
            {
                return false;
            }

            // No implicit conversion to enum.  Have to do this check here since calling GetTypeCode on an enum will
            // return the typecode of the underlying type which screws us up.
            return !(sourceType.IsEnum | destType.IsEnum)
                && EmitImplicitNumericConvert(sourceType, destType, ilg);
        }

        /// <summary>
        /// Emits an implicit numeric conversion (when <paramref name="ilg"/> is supplied) and
        /// returns whether the conversion is valid.
        /// </summary>
        /// <param name="sourceType">The source CLR type.</param>
        /// <param name="destType">The target CLR type.</param>
        /// <param name="ilg">The IL generator, or <see langword="null"/> to test feasibility only.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
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

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="short"/>.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToInt16(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16;
        }

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="ushort"/>.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToUInt16(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.UInt16;
        }

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="int"/>.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToInt32(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Char
                or TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32;
        }

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="uint"/>.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToUInt32(TypeCode sourceTypeCode)
        {
            return sourceTypeCode is TypeCode.Char
                or TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.UInt32;
        }

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="double"/>,
        /// emitting the appropriate <c>conv</c> opcode when it does.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <param name="ilg">The IL generator, or <see langword="null"/> to test feasibility only.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToDouble(TypeCode sourceTypeCode, FleeILGenerator? ilg)
        {
            if (sourceTypeCode is TypeCode.Char
                or TypeCode.SByte
                or TypeCode.Byte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                or TypeCode.Single
                or TypeCode.Int64)
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

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="float"/>,
        /// emitting the appropriate <c>conv</c> opcode when it does.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <param name="ilg">The IL generator, or <see langword="null"/> to test feasibility only.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToSingle(TypeCode sourceTypeCode, FleeILGenerator? ilg)
        {
            if (sourceTypeCode is TypeCode.Char
                or TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                or TypeCode.Int64)
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

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="long"/>,
        /// emitting the appropriate <c>conv</c> opcode when it does.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <param name="ilg">The IL generator, or <see langword="null"/> to test feasibility only.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
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

        /// <summary>
        /// Returns whether <paramref name="sourceTypeCode"/> implicitly converts to <see cref="ulong"/>,
        /// emitting the appropriate <c>conv</c> opcode when it does.
        /// </summary>
        /// <param name="sourceTypeCode">The source type code.</param>
        /// <param name="ilg">The IL generator, or <see langword="null"/> to test feasibility only.</param>
        /// <returns><see langword="true"/> when convertible.</returns>
        private static bool ImplicitConvertToUInt64(TypeCode sourceTypeCode, FleeILGenerator? ilg)
        {
            if (sourceTypeCode is TypeCode.Char or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32)
            {
                EmitConvert(ilg, OpCodes.Conv_U8);
                return true;
            }
            return sourceTypeCode == TypeCode.UInt64;
        }

        /// <summary>
        /// Emits <paramref name="convertOpcode"/> on <paramref name="ilg"/> when it isn't <see langword="null"/>.
        /// </summary>
        /// <param name="ilg">The IL generator (optional).</param>
        /// <param name="convertOpcode">The conversion opcode to emit.</param>
        private static void EmitConvert(FleeILGenerator? ilg, OpCode convertOpcode)
        {
            ilg?.Emit(convertOpcode);
        }

        /// <summary>
        /// Returns the result type for a binary operation between two primitives.
        /// </summary>
        /// <param name="t1">First operand type.</param>
        /// <param name="t2">Second operand type.</param>
        /// <returns>The result type, or <see langword="null"/> when either operand isn't a tracked primitive.</returns>
        public static Type? GetBinaryResultType(Type t1, Type t2)
        {
            int index1 = GetTypeIndex(t1);
            int index2 = GetTypeIndex(t2);

            return index1 == -1 | index2 == -1 ? null : OurBinaryResultTable[index1, index2];
        }

        /// <summary>
        /// Scores the implicit-convert relationship between two types. Lower is closer.
        /// Used by overload resolution to pick the best candidate.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The target type.</param>
        /// <returns>The score.</returns>
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

        /// <summary>
        /// Maps a value-type to a numeric promotion order; used to score conversions between
        /// primitive value types.
        /// </summary>
        /// <param name="t">The value type.</param>
        /// <returns>The score; <c>-1</c> when not a tracked primitive.</returns>
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

        /// <summary>
        /// Returns the score for a reference-type-to-reference-type conversion: a fixed cost
        /// for interfaces, otherwise the inheritance distance.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The target type.</param>
        /// <returns>The score.</returns>
        private static int GetReferenceTypeImplicitConvertScore(Type sourceType, Type destType)
        {
            return destType.IsInterface ? 100 : GetInheritanceDistance(sourceType, destType);
        }

        /// <summary>
        /// Returns the count of <see cref="Type.BaseType"/> hops needed to walk from
        /// <paramref name="sourceType"/> up to <paramref name="destType"/>, scaled by 1000.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The target ancestor type.</param>
        /// <returns>The scaled hop count.</returns>
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

        /// <summary>
        /// Scores a <see cref="Null"/> conversion against <paramref name="t"/> as the inverse
        /// distance to <see cref="object"/>: closer-to-Object types score higher because they
        /// are less specific.
        /// </summary>
        /// <param name="t">The destination type.</param>
        /// <returns>The score.</returns>
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
