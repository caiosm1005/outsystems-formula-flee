using Flee.PublicTypes;

namespace Flee.Parsing
{
    internal class RealPattern : CustomTokenPattern
    {
        public RealPattern(int id, string name, PatternType type, string pattern) : base(id, name, type, pattern)
        {
        }

        protected override void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;

            char digitsBeforePattern = (options.RequireDigitsBeforeDecimalPoint ? '+' : '*');

            pattern = string.Format(pattern, digitsBeforePattern, options.DecimalSeparator);

            this.SetData(id, name, type, pattern);
        }
    }
}
