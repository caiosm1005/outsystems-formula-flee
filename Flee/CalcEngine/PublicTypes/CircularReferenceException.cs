namespace Flee.CalcEngine.PublicTypes
{
    /// <summary>
    /// Thrown when an expression's dependencies form a cycle (e.g. <c>A</c> depends on
    /// <c>B</c> and <c>B</c> depends on <c>A</c>), preventing the engine from producing a
    /// topological recalculation order.
    /// </summary>
    public class CircularReferenceException : Exception
    {
        private readonly string? _myCircularReferenceSource;

        /// <summary>
        /// Initializes a new instance with no specific source identified.
        /// </summary>
        internal CircularReferenceException()
        {
        }

        /// <summary>
        /// Initializes a new instance attributing the cycle to <paramref name="circularReferenceSource"/>.
        /// </summary>
        /// <param name="circularReferenceSource">The expression name where the cycle was detected.</param>
        internal CircularReferenceException(string circularReferenceSource)
        {
            _myCircularReferenceSource = circularReferenceSource;
        }

        /// <summary>
        /// Gets the message for this exception. Includes the source expression name when one
        /// was provided.
        /// </summary>
        public override string Message => _myCircularReferenceSource == null
            ? "Circular reference detected in calculation engine"
            : $"Circular reference detected in calculation engine at '{_myCircularReferenceSource}'";
    }
}
