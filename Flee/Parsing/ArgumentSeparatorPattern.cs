using Flee.PublicTypes;

namespace Flee.Parsing
{
    internal class ArgumentSeparatorPattern : CustomTokenPattern
    {
        public ArgumentSeparatorPattern(int id, string name, PatternType type, string pattern) : base(id, name, type, pattern)
        {
        }

        protected override void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;
            this.SetData(id, name, type, options.FunctionArgumentSeparator.ToString());
        }
    }
}
