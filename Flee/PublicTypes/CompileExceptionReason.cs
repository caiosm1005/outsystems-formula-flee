namespace Flee.PublicTypes
{
    /// <summary>
    /// Identifies why an expression failed to compile.
    /// </summary>
    /// <remarks>
    /// Set on <see cref="ExpressionCompileException.Reason"/> so callers can branch on the kind of failure
    /// rather than parsing the message text.
    /// </remarks>
    public enum CompileExceptionReason
    {
        /// <summary>
        /// The expression text could not be parsed.
        /// </summary>
        SyntaxError,

        /// <summary>
        /// A literal constant exceeded the range of its target numeric type.
        /// </summary>
        ConstantOverflow,

        /// <summary>
        /// An operator or function was applied to operands of incompatible types.
        /// </summary>
        TypeMismatch,

        /// <summary>
        /// An identifier referenced an unknown variable, type, or member.
        /// </summary>
        UndefinedName,

        /// <summary>
        /// A function used in a value position has no return value.
        /// </summary>
        FunctionHasNoReturnValue,

        /// <summary>
        /// An explicit cast targeted a type that has no valid conversion from the source type.
        /// </summary>
        InvalidExplicitCast,

        /// <summary>
        /// An overload could not be uniquely resolved.
        /// </summary>
        AmbiguousMatch,

        /// <summary>
        /// A non-public member was referenced where the access policy disallows it.
        /// </summary>
        AccessDenied,

        /// <summary>
        /// A literal value (e.g. a date or time-span) could not be parsed in the configured format.
        /// </summary>
        InvalidFormat
    }
}
