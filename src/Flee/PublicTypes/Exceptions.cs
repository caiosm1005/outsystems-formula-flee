using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    public enum CompileExceptionReason
    {
        SyntaxError,
        ConstantOverflow,
        TypeMismatch,
        UndefinedName,
        FunctionHasNoReturnValue,
        InvalidExplicitCast,
        AmbiguousMatch,
        AccessDenied,
        InvalidFormat
    }

    [Serializable()]
    public sealed class ExpressionCompileException : Exception
    {
        private readonly CompileExceptionReason _myReason;
        internal ExpressionCompileException(string message, CompileExceptionReason reason) : base(message)
        {
            _myReason = reason;
        }

        internal ExpressionCompileException(ParserLogException parseException) : base(string.Empty, parseException)
        {
            _myReason = CompileExceptionReason.SyntaxError;
        }

        public override string Message
        {
            get
            {
                if (_myReason == CompileExceptionReason.SyntaxError)
                {
                    Exception innerEx = InnerException;
                    string msg = $"{Utility.GetCompileErrorMessage(CompileErrorResourceKeys.SyntaxError)}: {innerEx.Message}";
                    return msg;
                }
                else
                {
                    return base.Message;
                }
            }
        }

        public CompileExceptionReason Reason => _myReason;
    }
}
