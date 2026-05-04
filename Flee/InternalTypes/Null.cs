namespace Flee.InternalTypes
{
    /// <summary>
    /// Sentinel type used internally to represent the literal <c>null</c> within the
    /// expression element tree. Lets the type system distinguish "no value" from
    /// "value of type <see cref="object"/>".
    /// </summary>
    internal class Null
    {
    }
}
