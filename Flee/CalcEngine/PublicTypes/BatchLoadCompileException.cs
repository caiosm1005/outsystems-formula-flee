using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    public class BatchLoadCompileException : Exception
    {
        internal BatchLoadCompileException(string atomName, string expressionText, ExpressionCompileException innerException) : base(
            $"Batch Load: The expression for atom '${atomName}' could not be compiled", innerException)
        {
            AtomName = atomName;
            ExpressionText = expressionText;
        }

        public string AtomName { get; }

        public string ExpressionText { get; }
    }
}
