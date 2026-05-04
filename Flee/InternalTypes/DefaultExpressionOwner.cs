namespace Flee.InternalTypes
{
    /// <summary>
    /// Singleton placeholder used as the expression owner when callers haven't supplied one.
    /// </summary>
    internal class DefaultExpressionOwner
    {
        private static readonly DefaultExpressionOwner OurInstance = new();

        /// <summary>
        /// Initializes the singleton. Private to enforce the singleton pattern.
        /// </summary>
        private DefaultExpressionOwner()
        {
        }

        /// <summary>
        /// Gets the singleton instance, exposed as <see cref="object"/> for use as an expression owner.
        /// </summary>
        public static object Instance => OurInstance;
    }
}
