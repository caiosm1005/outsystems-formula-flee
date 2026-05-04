namespace Flee.Resources
{
    /// <summary>
    /// Resource keys for compile-error messages stored in <c>CompileErrors.resx</c>. Keep in sync
    /// with the resource file. Long key names are split with concatenation purely to satisfy the
    /// 120-char line-length limit; the resulting string values are unchanged.
    /// </summary>
    internal class CompileErrorResourceKeys
    {
        /// <summary>"Could not resolve type {0}."</summary>
        public const string CouldNotResolveType = "CouldNotResolveType";

        /// <summary>"Cannot convert from type {0} to type {1}."</summary>
        public const string CannotConvertType = "CannotConvertType";

        /// <summary>The first argument to a conditional expression must be boolean.</summary>
        public const string FirstArgNotBoolean = "FirstArgNotBoolean";

        /// <summary>Two operands aren't convertible to each other.</summary>
        public const string NeitherArgIsConvertibleToTheOther = "NeitherArgIsConvertibleToTheOther";

        /// <summary>A literal value cannot be represented in its target type.</summary>
        public const string ValueNotRepresentableInType = "ValueNotRepresentableInType";

        /// <summary>The <c>in</c> operator's right-hand side isn't a known collection type.</summary>
        public const string SearchArgIsNotKnownCollectionType = "SearchArgIsNotKnownCollectionType";

        /// <summary>The operand can't be converted to the collection's element type.</summary>
        public const string OperandNotConvertibleToCollectionType = "OperandNotConvertibleToCollectionType";

        /// <summary>Type isn't an array and has no matching indexer.</summary>
        public const string TypeNotArrayAndHasNoIndexerOfType = "TypeNotArrayAndHasNoIndexerOfType";

        /// <summary>Array indexers must be of a specific type (e.g. <see cref="int"/>).</summary>
        public const string ArrayIndexersMustBeOfType = "ArrayIndexersMustBeOfType";

        /// <summary>Function call is ambiguous between two or more overloads.</summary>
        public const string AmbiguousCallOfFunction = "AmbiguousCallOfFunction";

        /// <summary>A namespace can't be used as a type.</summary>
        public const string NamespaceCannotBeUsedAsType = "NamespaceCannotBeUsedAsType";

        /// <summary>A type can't be used as an expression value.</summary>
        public const string TypeCannotBeUsedAsAnExpression = "TypeCannotBeUsedAsAnExpression";

        /// <summary>Static member access requires a type reference, not an instance reference.</summary>
        public const string StaticMemberCannotBeAccessedWithInstanceReference
            = "StaticMemberCannotBeAccessedWithInstanceReference";

        /// <summary>Instance member access requires an object reference.</summary>
        public const string ReferenceToNonSharedMemberRequiresObjectReference
            = "ReferenceToNonSharedMemberRequiresObjectReference";

        /// <summary>A function used as a value has no return value.</summary>
        public const string FunctionHasNoReturnValue = "FunctionHasNoReturnValue";

        /// <summary>Operation isn't defined for a single operand type.</summary>
        public const string OperationNotDefinedForType = "OperationNotDefinedForType";

        /// <summary>Operation isn't defined for the given pair of operand types.</summary>
        public const string OperationNotDefinedForTypes = "OperationNotDefinedForTypes";

        /// <summary>The expression's value can't be converted to its declared result type.</summary>
        public const string CannotConvertTypeToExpressionResult = "CannotConvertTypeToExpressionResult";

        /// <summary>Overloaded operator resolution is ambiguous.</summary>
        public const string AmbiguousOverloadedOperator = "AmbiguousOverloadedOperator";

        /// <summary>No identifier with the given name exists.</summary>
        public const string NoIdentifierWithName = "NoIdentifierWithName";

        /// <summary>No identifier with the given name exists on the given type.</summary>
        public const string NoIdentifierWithNameOnType = "NoIdentifierWithNameOnType";

        /// <summary>Identifier matches more than one accessible member.</summary>
        public const string IdentifierIsAmbiguous = "IdentifierIsAmbiguous";

        /// <summary>Identifier matches more than one accessible member on a given type.</summary>
        public const string IdentifierIsAmbiguousOnType = "IdentifierIsAmbiguousOnType";

        /// <summary>Cannot reference a calc-engine atom outside a calc-engine context.</summary>
        public const string CannotReferenceCalcEngineAtomWithoutCalcEngine
            = "CannotReferenceCalcEngineAtomWithoutCalcEngine";

        /// <summary>Calc engine doesn't contain the named atom.</summary>
        public const string CalcEngineDoesNotContainAtom = "CalcEngineDoesNotContainAtom";

        /// <summary>Function name doesn't resolve.</summary>
        public const string UndefinedFunction = "UndefinedFunction";

        /// <summary>Function name doesn't resolve on the given type.</summary>
        public const string UndefinedFunctionOnType = "UndefinedFunctionOnType";

        /// <summary>Function exists but no overload is accessible from the expression.</summary>
        public const string NoAccessibleMatches = "NoAccessibleMatches";

        /// <summary>Function exists on the given type but no overload is accessible.</summary>
        public const string NoAccessibleMatchesOnType = "NoAccessibleMatchesOnType";

        /// <summary>Cannot parse a literal value into the target type.</summary>
        public const string CannotParseType = "CannotParseType";

        /// <summary>Multidimensional array indexing isn't supported.</summary>
        public const string MultiArrayIndexNotSupported = "MultiArrayIndexNotSupported";

        // Grammatica

        /// <summary>The parser encountered an unexpected token.</summary>
        public const string UnexpectedToken = "UNEXPECTED_TOKEN";

        /// <summary>I/O error in the parser stream.</summary>
        public const string IO = "IO";

        /// <summary>Unexpected end of input.</summary>
        public const string UnexpectedEof = "UNEXPECTED_EOF";

        /// <summary>Unexpected character in the input.</summary>
        public const string UnexpectedChar = "UNEXPECTED_CHAR";

        /// <summary>Tokenizer rejected the input.</summary>
        public const string InvalidToken = "INVALID_TOKEN";

        /// <summary>Generic analyzer error.</summary>
        public const string Analysis = "ANALYSIS";

        /// <summary>Format template for "line N, column M".</summary>
        public const string LineColumn = "LineColumn";

        /// <summary>"Syntax error".</summary>
        public const string SyntaxError = "SyntaxError";

        /// <summary>
        /// Private to enforce the static-only constants pattern.
        /// </summary>
        private CompileErrorResourceKeys()
        {
        }
    }
}
