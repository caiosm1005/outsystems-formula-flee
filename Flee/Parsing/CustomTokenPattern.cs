using Flee.PublicTypes;

namespace Flee.Parsing
{
    /// <summary>
    /// Base class for token patterns whose final form depends on the
    /// <see cref="ExpressionContext"/> (e.g. culture-driven separators). Each concrete subclass
    /// rewrites the token via <see cref="ComputeToken"/> when the parser is constructed.
    /// </summary>
    /// <param name="id">The token id.</param>
    /// <param name="name">The token name.</param>
    /// <param name="type">The token type.</param>
    /// <param name="pattern">The placeholder pattern.</param>
    internal abstract class CustomTokenPattern(int id, string name, TokenPattern.PatternType type, string pattern)
        : TokenPattern(id, name, type, pattern)
    {
        /// <summary>
        /// Initializes the pattern by delegating to <see cref="ComputeToken"/>.
        /// </summary>
        /// <param name="id">The token id.</param>
        /// <param name="name">The token name.</param>
        /// <param name="type">The token type.</param>
        /// <param name="pattern">The placeholder pattern.</param>
        /// <param name="context">The expression context.</param>
        public void Initialize(
            int id,
            string name,
            PatternType type,
            string pattern,
            ExpressionContext context)
        {
            ComputeToken(id, name, type, pattern, context);
        }

        /// <summary>
        /// Subclasses replace the placeholder pattern with the actual literal/regex form
        /// computed from <paramref name="context"/>.
        /// </summary>
        /// <param name="id">The token id.</param>
        /// <param name="name">The token name.</param>
        /// <param name="type">The token type.</param>
        /// <param name="pattern">The placeholder pattern.</param>
        /// <param name="context">The expression context.</param>
        protected abstract void ComputeToken(
            int id,
            string name,
            PatternType type,
            string pattern,
            ExpressionContext context);
    }
}
