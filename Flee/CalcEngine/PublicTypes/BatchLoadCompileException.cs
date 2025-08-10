using Flee.PublicTypes;

namespace Flee.CalcEngine.PublicTypes
{
    public class BatchLoadCompileException : Exception
    {

        private readonly string _myAtomName;

        private readonly string _myExpressionText;
        internal BatchLoadCompileException(string atomName, string expressionText, ExpressionCompileException innerException) : base(
            $"Batch Load: The expression for atom '${atomName}' could not be compiled", innerException)
        {
            _myAtomName = atomName;
            _myExpressionText = expressionText;
        }

        public string AtomName => _myAtomName;

        public string ExpressionText => _myExpressionText;
    }
}
