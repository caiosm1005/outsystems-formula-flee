using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public sealed class ExpressionCompileException : Exception
    {
        internal ExpressionCompileException(string message, CompileExceptionReason reason) : base(message)
        {
            Reason = reason;
        }

        internal ExpressionCompileException(ParserLogException parseException) : base(string.Empty, parseException)
        {
            Reason = CompileExceptionReason.SyntaxError;
        }

        public override string Message
        {
            get
            {
                if (Reason == CompileExceptionReason.SyntaxError)
                {
                    Exception? innerEx = InnerException;
                    string msg = $"{Utility.GetCompileErrorMessage(CompileErrorResourceKeys.SyntaxError)}: {innerEx?.Message}";
                    return msg;
                }
                else
                {
                    return base.Message;
                }
            }
        }

        public CompileExceptionReason Reason { get; }
    }
}
