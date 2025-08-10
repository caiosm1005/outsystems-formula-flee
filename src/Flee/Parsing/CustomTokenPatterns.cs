using Flee.PublicTypes;

namespace Flee.Parsing
{
    internal abstract class CustomTokenPattern : TokenPattern
    {
        protected CustomTokenPattern(int id, string name, PatternType type, string pattern) : base(id, name, type, pattern)
        {
        }

        public void Initialize(int id, string name, PatternType type, string pattern, ExpressionContext context)
        {
            this.ComputeToken(id, name, type, pattern, context);
        }

        protected abstract void ComputeToken(int id, string name, PatternType type, string pattern, ExpressionContext context);
    }
}
