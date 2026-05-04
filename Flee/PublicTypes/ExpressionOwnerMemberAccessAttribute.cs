namespace Flee.PublicTypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ExpressionOwnerMemberAccessAttribute(bool allowAccess) : Attribute
    {
        internal bool AllowAccess { get; } = allowAccess;
    }
}
