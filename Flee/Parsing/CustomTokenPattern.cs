using Flee.PublicTypes;

namespace Flee.Parsing
{
    internal abstract class CustomTokenPattern(int id, string name, TokenPattern.PatternType type, string pattern) : TokenPattern(id, name, type, pattern)
    {
        public void Initialize(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            ComputeToken(id, name, type, pattern, context);
        }

        protected abstract void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context);
    }
}
