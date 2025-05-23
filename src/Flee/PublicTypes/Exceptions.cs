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
        private static string FormatMessage(string elementName, string messageKey, object[] arguments)
        {
            string messageTemplate = FleeResourceManager.Instance.GetCompileErrorString(messageKey);
            string message = string.Format(messageTemplate, arguments);
            message = string.Concat(elementName, ": ", message);
            return message;
        }

        private readonly CompileExceptionReason _myReason;

        internal ExpressionCompileException(string message, CompileExceptionReason reason) : base(message)
        {
            _myReason = reason;
        }

        internal ExpressionCompileException(ParserLogException parseException) : base(string.Empty, parseException)
        {
            _myReason = CompileExceptionReason.SyntaxError;
        }

        internal ExpressionCompileException(string elementName, string messageKey, CompileExceptionReason reason,
            params object[] arguments) : base(FormatMessage(elementName, messageKey, arguments))
        {
            _myReason = reason;
        }

        public override string Message
        {
            get
            {
                if (_myReason == CompileExceptionReason.SyntaxError)
                {
                    string message = Utility.GetCompileErrorMessage(CompileErrorResourceKeys.SyntaxError);
                    if (InnerException != null && !string.IsNullOrEmpty(InnerException.Message))
                    {
                        message += ": " + InnerException.Message;
                    }
                    return message;
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
