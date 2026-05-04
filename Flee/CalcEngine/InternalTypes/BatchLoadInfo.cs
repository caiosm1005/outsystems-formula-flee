using Flee.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{
    /// <summary>
    /// Captures the data needed to compile a single expression during a batch-load operation:
    /// the expression's name in the engine, its source text, and the context to compile it
    /// against.
    /// </summary>
    /// <param name="name">The expression name.</param>
    /// <param name="text">The expression source text.</param>
    /// <param name="context">The compilation context.</param>
    internal class BatchLoadInfo(string name, string text, ExpressionContext context)
    {
        /// <summary>The expression's name in the engine.</summary>
        public string Name = name;

        /// <summary>The expression source text.</summary>
        public string ExpressionText = text;

        /// <summary>The compilation context.</summary>
        public ExpressionContext Context = context;
    }
}
