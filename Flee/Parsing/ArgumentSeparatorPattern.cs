using Flee.PublicTypes;

namespace Flee.Parsing
{
    internal class ArgumentSeparatorPattern(int id, string name, TokenPattern.PatternType type, string pattern) : CustomTokenPattern(id, name, type, pattern)
    {
        protected override void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ExpressionParserOptions options = context.ParserOptions;
            SetData(id, name, type, options.FunctionArgumentSeparator.ToString());
        }
    }
}
