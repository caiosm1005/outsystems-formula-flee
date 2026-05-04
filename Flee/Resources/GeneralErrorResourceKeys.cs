namespace Flee.Resources
{
    /// <summary>
    /// Resource keys for general (non-compile-time) error messages stored in
    /// <c>GeneralErrors.resx</c>. Keep in sync with the resource file.
    /// </summary>
    internal class GeneralErrorResourceKeys
    {
        /// <summary>The named CLR type isn't accessible from expressions.</summary>
        public const string TypeNotAccessibleToExpression = "TypeNotAccessibleToExpression";

        /// <summary>A variable with the same name has already been defined.</summary>
        public const string VariableWithNameAlreadyDefined = "VariableWithNameAlreadyDefined";

        /// <summary>The named variable hasn't been defined.</summary>
        public const string UndefinedVariable = "UndefinedVariable";

        /// <summary>The supplied variable name is invalid.</summary>
        public const string InvalidVariableName = "InvalidVariableName";

        /// <summary>The new variable's type couldn't be inferred from its value.</summary>
        public const string CannotDetermineNewVariableType = "CannotDetermineNewVariableType";

        /// <summary>The supplied variable value isn't assignable to the declared type.</summary>
        public const string VariableValueNotAssignableToType = "VariableValueNotAssignableToType";

        /// <summary>Failed to find a public, static method with the given name on a type.</summary>
        public const string CouldNotFindPublicStaticMethodOnType = "CouldNotFindPublicStaticMethodOnType";

        /// <summary>Only public, static methods can be imported as expression functions.</summary>
        public const string OnlyPublicStaticMethodsCanBeImported = "OnlyPublicStaticMethodsCanBeImported";

        /// <summary>The supplied namespace name is invalid (e.g. empty).</summary>
        public const string InvalidNamespaceName = "InvalidNamespaceName";

        /// <summary>A new owner's type isn't assignable to the original owner's type.</summary>
        public const string NewOwnerTypeNotAssignableToCurrentOwner = "NewOwnerTypeNotAssignableToCurrentOwner";

        /// <summary>
        /// Private to enforce the static-only constants pattern.
        /// </summary>
        private GeneralErrorResourceKeys()
        {
        }
    }
}
