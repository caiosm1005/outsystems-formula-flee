using Flee.PublicTypes;

namespace Flee.Parsing
{
    internal class RealPattern(int id, string name, TokenPattern.PatternType type, string pattern) : CustomTokenPattern(id, name, type, pattern)
    {
        protected override void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;

            char digitsBeforePattern = options.RequireDigitsBeforeDecimalPoint ? '+' : '*';

            pattern = string.Format(pattern, digitsBeforePattern, options.DecimalSeparator);

            SetData(id, name, type, pattern);
        }
    }
}
