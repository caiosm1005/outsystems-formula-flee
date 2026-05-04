namespace Flee.PublicTypes
{
    /// <summary>
    /// Marks a field, method, or property on an expression owner as visible (or hidden) to
    /// expressions, overriding the default access policy in <see cref="ExpressionOptions.OwnerMemberAccess"/>.
    /// </summary>
    /// <param name="allowAccess">
    /// <see langword="true"/> to expose the member to expressions, <see langword="false"/> to hide it.
    /// </param>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class ExpressionOwnerMemberAccessAttribute(bool allowAccess) : Attribute
    {
        /// <summary>
        /// Gets a value indicating whether the decorated member is accessible from expressions.
        /// </summary>
        internal bool AllowAccess { get; } = allowAccess;
    }
}
