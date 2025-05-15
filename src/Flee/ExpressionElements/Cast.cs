using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    internal class CastElement : ExpressionElement
    {
        private readonly ExpressionElement _myCastExpression;
        private readonly Type _myDestType;
        public CastElement(ExpressionElement castExpression, string[] destTypeParts, bool isArray, IServiceProvider services)
        {
            _myCastExpression = castExpression;

            _myDestType = GetDestType(destTypeParts, services);

            if (_myDestType == null)
            {
                ThrowCompileException(CompileErrorResourceKeys.CouldNotResolveType, CompileExceptionReason.UndefinedName, GetDestTypeString(destTypeParts, isArray));
            }

            if (isArray)
            {
                _myDestType = _myDestType.MakeArrayType();
            }

            if (!IsValidCast(_myCastExpression.ResultType, _myDestType))
            {
                ThrowInvalidCastException();
            }
        }

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
        /// Resolve the type we are casting to
        /// </summary>
        /// <param name="destTypeParts"></param>
        /// <param name="services"></param>
        /// <returns></returns>
        private static Type GetDestType(string[] destTypeParts, IServiceProvider services)
        {
            ExpressionContext context = (ExpressionContext)services.GetService(typeof(ExpressionContext));

            Type t = null;

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
            t = context.Imports.FindType(destTypeParts);

            if (t != null)
            {
                return t;
            }

            return null;
        }

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
            else if (ImplicitConverter.EmitImplicitConvert(sourceType, destType, null, null))
            {
                // Cast is already implicitly valid
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
                // If we get here then the cast always fails since we are either casting one value type to another
                // or a value type to an invalid reference type
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

        private MethodInfo GetExplictOverloadedOperator(Type sourceType, Type destType)
        {
            ExplicitOperatorMethodBinder binder = new(destType, sourceType);

            // Look for an operator on the source type and dest types
            MethodInfo miSource = Utility.GetOverloadedOperator("Explicit", sourceType, binder, sourceType);
            MethodInfo miDest = Utility.GetOverloadedOperator("Explicit", destType, binder, sourceType);

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

        private bool IsValidExplicitEnumCast(Type sourceType, Type destType)
        {
            sourceType = GetUnderlyingEnumType(sourceType);
            destType = GetUnderlyingEnumType(destType);
            return IsValidCast(sourceType, destType);
        }

        private static bool IsValidExplicitReferenceCast(Type sourceType, Type destType)
        {
            Debug.Assert(!sourceType.IsValueType & !destType.IsValueType, "expecting reference types");

            if (ReferenceEquals(sourceType, typeof(object)))
            {
                // From object to any other reference-type
                return true;
            }
            else if (sourceType.IsArray & destType.IsArray)
            {
                // From an array-type S with an element type SE to an array-type T with an element type TE,
                // provided all of the following are true:

                // S and T have the same number of dimensions
                if (sourceType.GetArrayRank() != destType.GetArrayRank())
                {
                    return false;
                }
                else
                {
                    Type SE = sourceType.GetElementType();
                    Type TE = destType.GetElementType();

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
                // From any class-type S to any interface-type T, provided S is not sealed and provided S does not implement T
                return !sourceType.IsSealed & !ImplementsInterface(sourceType, destType);
            }
            else if (sourceType.IsInterface & destType.IsClass)
            {
                // From any interface-type S to any class-type T, provided T is not sealed or provided T implements S.
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

        private static bool IsBaseType(Type target, Type potentialBase)
        {
            Type current = target;
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

        private static bool ImplementsInterface(Type target, Type interfaceType)
        {
            Type[] interfaces = target.GetInterfaces();
            return Array.IndexOf(interfaces, interfaceType) != -1;
        }

        private void ThrowInvalidCastException()
        {
            ThrowCompileException(CompileErrorResourceKeys.CannotConvertType, CompileExceptionReason.InvalidExplicitCast, _myCastExpression.ResultType.Name, _myDestType.Name);
        }

        private static Type GetUnderlyingEnumType(Type t)
        {
            if (t.IsEnum)
            {
                return Enum.GetUnderlyingType(t);
            }
            else
            {
                return t;
            }
        }

        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _myCastExpression.Emit(ilg, services);

            Type sourceType = _myCastExpression.ResultType;
            Type destType = _myDestType;

            EmitCast(ilg, sourceType, destType, services);
        }

        private void EmitCast(FleeILGenerator ilg, Type sourceType, Type destType, IServiceProvider services)
        {
            MethodInfo explicitOperator = GetExplictOverloadedOperator(sourceType, destType);

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
            else if (ImplicitConverter.EmitImplicitConvert(sourceType, destType, ilg, services))
            {
                // Implicit numeric cast; do nothing
                return;
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

        public override Type ResultType => _myDestType;
    }
}
