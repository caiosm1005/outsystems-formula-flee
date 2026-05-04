using Flee.InternalTypes;
using Flee.Parsing;
using Flee.Resources;

namespace Flee.PublicTypes
{
    /// <summary>
    /// Thrown when an expression fails to compile. <see cref="Reason"/> categorizes the failure.
    /// </summary>
    public sealed class ExpressionCompileException : Exception
    {
        /// <summary>
        /// Initializes a new instance with an explicit message and reason.
        /// </summary>
        /// <param name="message">The error message describing the compile failure.</param>
        /// <param name="reason">The category of failure.</param>
        internal ExpressionCompileException(string message, CompileExceptionReason reason) : base(message)
        {
            Reason = reason;
        }

        /// <summary>
        /// Initializes a new instance wrapping a parser-level exception as a syntax error.
        /// </summary>
        /// <param name="parseException">The underlying parser exception.</param>
        internal ExpressionCompileException(ParserLogException parseException) : base(string.Empty, parseException)
        {
            Reason = CompileExceptionReason.SyntaxError;
        }

        /// <summary>
        /// Gets the message for this exception. For <see cref="CompileExceptionReason.SyntaxError"/>
        /// the message is composed from the syntax-error template and the inner parser message;
        /// otherwise the base message is returned verbatim.
        /// </summary>
        public override string Message
        {
            get
            {
                if (Reason == CompileExceptionReason.SyntaxError)
                {
                    Exception? innerEx = InnerException;
                    string template = Utility.GetCompileErrorMessage(CompileErrorResourceKeys.SyntaxError);
                    string msg = $"{template}: {innerEx?.Message}";
                    return msg;
                }
                else
                {
                    return base.Message;
                }
            }
        }

        /// <summary>
        /// Gets the category of compile failure that produced this exception.
        /// </summary>
        public CompileExceptionReason Reason { get; }
    }
}
