using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    internal class BatchLoadInfo(string name, string text, ExpressionContext context)
    {
        public string Name = name;
        public string ExpressionText = text;

        public ExpressionContext Context = context;
    }
}
