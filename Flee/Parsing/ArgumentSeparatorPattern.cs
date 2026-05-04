using Flee.PublicTypes;

namespace Flee.Parsing
{
    /// <summary>
    /// Token pattern whose literal text is taken from
    /// <see cref="ExpressionParserOptions.FunctionArgumentSeparator"/> at compile time. Lets the
    /// argument separator follow the configured culture (e.g. <c>,</c> versus <c>;</c>).
    /// </summary>
    /// <param name="id">The token id.</param>
    /// <param name="name">The token name.</param>
    /// <param name="type">The token type.</param>
    /// <param name="pattern">The placeholder pattern (replaced at compile time).</param>
    internal class ArgumentSeparatorPattern(int id, string name, TokenPattern.PatternType type, string pattern)
        : CustomTokenPattern(id, name, type, pattern)
    {
        /// <summary>
        /// Replaces the placeholder pattern with the configured argument-separator character.
        /// </summary>
        /// <param name="id">The token id.</param>
        /// <param name="name">The token name.</param>
        /// <param name="type">The token type.</param>
        /// <param name="pattern">The original pattern (ignored — replaced).</param>
        /// <param name="context">The expression context (used for parser options).</param>
        protected override void ComputeToken(
            int id,
            string name,
            PatternType type,
            string pattern,
            ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;
            SetData(id, name, type, options.FunctionArgumentSeparator.ToString());
        }
    }
}
