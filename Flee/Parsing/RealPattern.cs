using Flee.PublicTypes;

namespace Flee.Parsing
{
    /// <summary>
    /// Token pattern for real-number literals whose regex is parameterized by the configured
    /// decimal separator and the "digits before the decimal point" flag.
    /// </summary>
    /// <param name="id">The token id.</param>
    /// <param name="name">The token name.</param>
    /// <param name="type">The token type.</param>
    /// <param name="pattern">The placeholder pattern with format slots.</param>
    internal class RealPattern(int id, string name, TokenPattern.PatternType type, string pattern)
        : CustomTokenPattern(id, name, type, pattern)
    {
        /// <summary>
        /// Substitutes the format slots in <paramref name="pattern"/> with the digits-before
        /// quantifier (<c>+</c> or <c>*</c>) and the configured decimal separator.
        /// </summary>
        /// <param name="id">The token id.</param>
        /// <param name="name">The token name.</param>
        /// <param name="type">The token type.</param>
        /// <param name="pattern">The placeholder pattern with format slots.</param>
        /// <param name="context">The expression context.</param>
        protected override void ComputeToken(
            int id,
            string name,
            PatternType type,
            string pattern,
            ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;

            char digitsBeforePattern = options.RequireDigitsBeforeDecimalPoint ? '+' : '*';

            pattern = string.Format(pattern, digitsBeforePattern, options.DecimalSeparator);

            SetData(id, name, type, pattern);
        }
    }
}
