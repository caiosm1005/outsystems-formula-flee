using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Explicit cast expression. Validates that the cast is legal at compile time and emits
    /// the appropriate IL: identity casts are no-ops, numerics get the right <c>conv</c> opcode,
    /// boxing/unboxing handle value↔reference, and user-defined <c>op_Explicit</c> overloads
    /// are honored.
    /// </summary>
    internal class CastElement : ExpressionElement
    {
        private readonly ExpressionElement _myCastExpression;
        private readonly Type _myDestType;

        /// <summary>
        /// Initializes a new cast around <paramref name="castExpression"/>.
        /// </summary>
        /// <param name="castExpression">The expression being cast.</param>
        /// <param name="destTypeParts">The dotted destination type name from the source.</param>
        /// <param name="isArray">Whether the destination type was followed by <c>[]</c>.</param>
        /// <param name="services">The compile services (used for type resolution).</param>
        public CastElement(
            ExpressionElement castExpression,
            string[] destTypeParts,
            bool isArray,
            IServiceProvider services)
        {
            _myCastExpression = castExpression;

            Type? destType = GetDestType(destTypeParts, services);

            if (destType == null)
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.CouldNotResolveType,
                    CompileExceptionReason.UndefinedName,
                    GetDestTypeString(destTypeParts, isArray));
            }

            _myDestType = destType!;

            if (isArray)
            {
                _myDestType = _myDestType.MakeArrayType();
            }

            if (!IsValidCast(_myCastExpression.ResultType, _myDestType))
            {
                ThrowInvalidCastException();
            }
        }

        /// <summary>
        /// Reconstructs the dotted destination type string from its parts, appending <c>[]</c>
        /// when the target was an array.
        /// </summary>
        /// <param name="parts">The dotted type-name parts.</param>
        /// <param name="isArray">Whether the type was an array.</param>
        /// <returns>The diagnostic string.</returns>
        private static string GetDestTypeString(string[] parts, bool isArray)
        {
            string s = string.Join(".", parts);

            if (isArray)
            {
                s += "[]";
            }

            return s;
        }

        /// <summary>
        /// Resolve the type we are casting to, falling back to imports when the dotted name
        /// isn't a built-in alias.
        /// </summary>
        /// <param name="destTypeParts">The dotted destination-type parts.</param>
        /// <param name="services">The compile services.</param>
        /// <returns>The CLR type, or <see langword="null"/> when unresolved.</returns>
        private static Type? GetDestType(string[] destTypeParts, IServiceProvider services)
        {
            ExpressionContext context = (ExpressionContext)services.GetService(typeof(ExpressionContext))!;

            Type? t = null;

            // Try to find a builtin type with the name
            if (destTypeParts.Length == 1)
            {
                t = ExpressionImports.GetBuiltinType(destTypeParts[0]);
            }

            if (t != null)
            {
                return t;
            }

            // Try to find the type in an import
            return context.Imports.FindType(destTypeParts);
        }

        /// <summary>
        /// Returns whether casting from <paramref name="sourceType"/> to <paramref name="destType"/>
        /// is allowed. Walks each cast category in turn (identity, implicit, numeric, enum,
        /// user-defined operator, value/reference shapes).
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The destination type.</param>
        /// <returns><see langword="true"/> when the cast is valid.</returns>
        private bool IsValidCast(Type sourceType, Type destType)
        {
            if (ReferenceEquals(sourceType, destType))
            {
                // Identity cast always succeeds
                return true;
            }
            else if (destType.IsAssignableFrom(sourceType))
            {
                // Cast is already implicitly valid
                return true;
            }
            else if (ImplicitConverter.EmitImplicitConvert(sourceType, destType, null))
            {
                // Cast is already implicitly valid
                return true;
            }
            else if (IsCastableNumericType(sourceType) & IsCastableNumericType(destType))
            {
                // Explicit cast of numeric types always succeeds
                return true;
            }
            else if (sourceType.IsEnum | destType.IsEnum)
            {
                return IsValidExplicitEnumCast(sourceType, destType);
            }
            else if (GetExplictOverloadedOperator(sourceType, destType) != null)
            {
                // Overloaded explict cast exists
                return true;
            }

            if (sourceType.IsValueType)
            {
                // If we get here then the cast always fails since we are either casting one
                // value type to another or a value type to an invalid reference type
                return false;
            }
            else
            {
                if (destType.IsValueType)
                {
                    // Reference type to value type
                    // Can only succeed if the reference type is a base of the value type or
                    // it is one of the interfaces the value type implements
                    Type[] interfaces = destType.GetInterfaces();
                    return IsBaseType(destType, sourceType) | Array.IndexOf(interfaces, sourceType) != -1;
                }
                else
                {
                    // Reference type to reference type
                    return IsValidExplicitReferenceCast(sourceType, destType);
                }
            }
        }

        /// <summary>
        /// Looks for a user-defined <c>op_Explicit</c> on either the source or the destination
        /// type, throwing when both define one (ambiguous).
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The destination type.</param>
        /// <returns>The chosen operator, or <see langword="null"/> when none is defined.</returns>
        private MethodInfo? GetExplictOverloadedOperator(Type sourceType, Type destType)
        {
            ExplicitOperatorMethodBinder binder = new(destType, sourceType);

            // Look for an operator on the source type and dest types
            MethodInfo? miSource = Utility.GetOverloadedOperator("Explicit", sourceType, binder, sourceType);
            MethodInfo? miDest = Utility.GetOverloadedOperator("Explicit", destType, binder, sourceType);

            if (miSource == null & miDest == null)
            {
                return null;
            }
            else if (miSource == null)
            {
                return miDest;
            }
            else if (miDest == null)
            {
                return miSource;
            }
            else
            {
                ThrowAmbiguousCallException(sourceType, destType, "Explicit");
                return null;
            }
        }

        /// <summary>
        /// Reduces an enum cast to a cast between the underlying integral types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The destination type.</param>
        /// <returns><see langword="true"/> when the underlying cast is valid.</returns>
        private bool IsValidExplicitEnumCast(Type sourceType, Type destType)
        {
            sourceType = GetUnderlyingEnumType(sourceType);
            destType = GetUnderlyingEnumType(destType);
            return IsValidCast(sourceType, destType);
        }

        /// <summary>
        /// Returns whether an explicit reference-to-reference conversion is allowed by the CLR rules
        /// (covering object, arrays, classes, and interfaces).
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The destination type.</param>
        /// <returns><see langword="true"/> when valid.</returns>
        private bool IsValidExplicitReferenceCast(Type sourceType, Type destType)
        {
            Debug.Assert(!sourceType.IsValueType & !destType.IsValueType, "expecting reference types");

            if (ReferenceEquals(sourceType, typeof(object)))
            {
                // From object to any other reference-type
                return true;
            }
            else if (sourceType.IsArray & destType.IsArray)
            {
                // From an array-type S with an element type SE to an array-type T with an
                // element type TE, provided all of the following are true:

                // S and T have the same number of dimensions
                if (sourceType.GetArrayRank() != destType.GetArrayRank())
                {
                    return false;
                }
                else
                {
                    Type SE = sourceType.GetElementType()!;
                    Type TE = destType.GetElementType()!;

                    // Both SE and TE are reference-types
                    if (SE.IsValueType | TE.IsValueType)
                    {
                        return false;
                    }
                    else
                    {
                        // An explicit reference conversion exists from SE to TE
                        return IsValidExplicitReferenceCast(SE, TE);
                    }
                }
            }
            else if (sourceType.IsClass & destType.IsClass)
            {
                // From any class-type S to any class-type T, provided S is a base class of T
                return IsBaseType(destType, sourceType);
            }
            else if (sourceType.IsClass & destType.IsInterface)
            {
                // From any class-type S to any interface-type T, provided S is not sealed and
                // provided S does not implement T
                return !sourceType.IsSealed & !ImplementsInterface(sourceType, destType);
            }
            else if (sourceType.IsInterface & destType.IsClass)
            {
                // From any interface-type S to any class-type T, provided T is not sealed or
                // provided T implements S.
                return !destType.IsSealed | ImplementsInterface(destType, sourceType);
            }
            else if (sourceType.IsInterface & destType.IsInterface)
            {
                // From any interface-type S to any interface-type T, provided S is not derived from T
                return !ImplementsInterface(sourceType, destType);
            }
            else
            {
                Debug.Assert(false, "unknown explicit cast");
            }

            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="potentialBase"/> is anywhere on
        /// <paramref name="target"/>'s base-type chain.
        /// </summary>
        /// <param name="target">The starting type.</param>
        /// <param name="potentialBase">The candidate base type.</param>
        /// <returns><see langword="true"/> when the relationship holds.</returns>
        private static bool IsBaseType(Type target, Type potentialBase)
        {
            Type? current = target;
            while (current != null)
            {
                if (ReferenceEquals(current, potentialBase))
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="target"/> implements <paramref name="interfaceType"/>.
        /// </summary>
        /// <param name="target">The candidate type.</param>
        /// <param name="interfaceType">The interface to look for.</param>
        /// <returns><see langword="true"/> when implemented.</returns>
        private static bool ImplementsInterface(Type target, Type interfaceType)
        {
            Type[] interfaces = target.GetInterfaces();
            return Array.IndexOf(interfaces, interfaceType) != -1;
        }

        /// <summary>
        /// Throws a "cannot convert" compile exception describing the rejected cast.
        /// </summary>
        private void ThrowInvalidCastException()
        {
            ThrowCompileException(
                CompileErrorResourceKeys.CannotConvertType,
                CompileExceptionReason.InvalidExplicitCast,
                _myCastExpression.ResultType.Name,
                _myDestType.Name);
        }

        /// <summary>
        /// Returns whether <paramref name="t"/> is a primitive type that participates in
        /// numeric casts (everything except <see cref="bool"/>).
        /// </summary>
        /// <param name="t">The candidate type.</param>
        /// <returns><see langword="true"/> when it qualifies.</returns>
        private static bool IsCastableNumericType(Type t)
        {
            return t.IsPrimitive & (!ReferenceEquals(t, typeof(bool)));
        }

        /// <summary>
        /// Returns the underlying integral type for an enum, or <paramref name="t"/> as-is when
        /// it isn't an enum.
        /// </summary>
        /// <param name="t">The type to inspect.</param>
        /// <returns>The underlying type.</returns>
        private static Type GetUnderlyingEnumType(Type t)
        {
            return t.IsEnum ? Enum.GetUnderlyingType(t) : t;
        }

        /// <summary>
        /// Emits the cast expression followed by the cast itself.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _myCastExpression.Emit(ilg, services);

            Type sourceType = _myCastExpression.ResultType;
            Type destType = _myDestType;

            EmitCast(ilg, sourceType, destType, services);
        }

        /// <summary>
        /// Emits the IL pattern that corresponds to the validated cast category.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The destination type.</param>
        /// <param name="services">The compile services.</param>
        private void EmitCast(FleeILGenerator ilg, Type sourceType, Type destType, IServiceProvider services)
        {
            MethodInfo? explicitOperator = GetExplictOverloadedOperator(sourceType, destType);

            if (ReferenceEquals(sourceType, destType))
            {
                // Identity cast; do nothing
                return;
            }
            else if (explicitOperator != null)
            {
                ilg.Emit(OpCodes.Call, explicitOperator);
            }
            else if (sourceType.IsEnum | destType.IsEnum)
            {
                EmitEnumCast(ilg, sourceType, destType, services);
            }
            else if (ImplicitConverter.EmitImplicitConvert(sourceType, destType, ilg))
            {
                // Implicit numeric cast; do nothing
                return;
            }
            else if (IsCastableNumericType(sourceType) & IsCastableNumericType(destType))
            {
                // Explicit numeric cast
                EmitExplicitNumericCast(ilg, sourceType, destType, services);
            }
            else if (sourceType.IsValueType)
            {
                Debug.Assert(!destType.IsValueType, "expecting reference type");
                ilg.Emit(OpCodes.Box, sourceType);
            }
            else
            {
                if (destType.IsValueType)
                {
                    // Reference type to value type
                    ilg.Emit(OpCodes.Unbox_Any, destType);
                }
                else
                {
                    // Reference type to reference type
                    if (!destType.IsAssignableFrom(sourceType))
                    {
                        // Only emit cast if it is an explicit cast
                        ilg.Emit(OpCodes.Castclass, destType);
                    }
                }
            }
        }

        /// <summary>
        /// Emits an enum cast, falling back to value-type box/unbox when one side isn't an enum,
        /// or to an underlying-type cast when both are enums.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destType">The destination type.</param>
        /// <param name="services">The compile services.</param>
        private void EmitEnumCast(FleeILGenerator ilg, Type sourceType, Type destType, IServiceProvider services)
        {
            if (!destType.IsValueType)
            {
                ilg.Emit(OpCodes.Box, sourceType);
            }
            else if (!sourceType.IsValueType)
            {
                ilg.Emit(OpCodes.Unbox_Any, destType);
            }
            else
            {
                sourceType = GetUnderlyingEnumType(sourceType);
                destType = GetUnderlyingEnumType(destType);
                EmitCast(ilg, sourceType, destType, services);
            }
        }

        /// <summary>
        /// Emits the appropriate <c>conv</c> opcode for an explicit numeric cast, picking
        /// checked/unsigned variants based on options and the source type's signedness.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="sourceType">The source numeric type.</param>
        /// <param name="destType">The destination numeric type.</param>
        /// <param name="services">The compile services (used to read <see cref="ExpressionOptions.Checked"/>).</param>
        private static void EmitExplicitNumericCast(
            FleeILGenerator ilg,
            Type sourceType,
            Type destType,
            IServiceProvider services)
        {
            TypeCode desttc = Type.GetTypeCode(destType);
            TypeCode sourcetc = Type.GetTypeCode(sourceType);
            bool unsigned = IsUnsignedType(sourceType);
            ExpressionOptions options = (ExpressionOptions)services.GetService(typeof(ExpressionOptions))!;
            bool @checked = options.Checked;
            OpCode op = OpCodes.Nop;

            switch (desttc)
            {
                case TypeCode.SByte:
                    op = unsigned & @checked
                        ? OpCodes.Conv_Ovf_I1_Un
                        : @checked ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_I1;
                    break;
                case TypeCode.Byte:
                    op = unsigned & @checked
                        ? OpCodes.Conv_Ovf_U1_Un
                        : @checked ? OpCodes.Conv_Ovf_U1 : OpCodes.Conv_U1;
                    break;
                case TypeCode.Int16:
                    op = unsigned & @checked
                        ? OpCodes.Conv_Ovf_I2_Un
                        : @checked ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_I2;
                    break;
                case TypeCode.UInt16:
                    op = unsigned & @checked
                        ? OpCodes.Conv_Ovf_U2_Un
                        : @checked ? OpCodes.Conv_Ovf_U2 : OpCodes.Conv_U2;
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
                        // Don't need to emit a convert for this case since, to the CLR, it is
                        // the same data type
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
                case TypeCode.Empty:
                    break;
                case TypeCode.Object:
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.Boolean:
                    break;
                case TypeCode.Char:
                    break;
                case TypeCode.Double:
                    break;
                case TypeCode.Decimal:
                    break;
                case TypeCode.DateTime:
                    break;
                case TypeCode.String:
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

        /// <summary>
        /// Returns whether <paramref name="t"/> is one of the unsigned integral primitives.
        /// </summary>
        /// <param name="t">The candidate type.</param>
        /// <returns><see langword="true"/> when unsigned.</returns>
        private static bool IsUnsignedType(Type t)
        {
            return Type.GetTypeCode(t) is TypeCode.Byte
                or TypeCode.UInt16
                or TypeCode.UInt32
                or TypeCode.UInt64;
        }

        /// <summary>
        /// Gets the destination type of the cast.
        /// </summary>
        public override Type ResultType => _myDestType;
    }
}
