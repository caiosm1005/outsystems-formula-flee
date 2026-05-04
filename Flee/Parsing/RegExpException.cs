using System.Text;


namespace Flee.Parsing
{
    /// <summary>
    /// A regular-expression exception. Thrown when a regular expression cannot be processed
    /// (or "compiled") successfully.
    /// </summary>
    /// <param name="type">The kind of error.</param>
    /// <param name="pos">The position in the pattern where the error occurred.</param>
    /// <param name="pattern">The pattern that produced the error.</param>
    internal class RegExpException(RegExpException.ErrorType type, int pos, string pattern) : Exception
    {
        /// <summary>
        /// Enumerates the kinds of regex compilation errors.
        /// </summary>
        public enum ErrorType
        {
            /// <summary>
            /// A character was read that didn't match the allowed set at the given position.
            /// </summary>
            UNEXPECTED_CHARACTER,

            /// <summary>
            /// More characters were expected in the pattern.
            /// </summary>
            UNTERMINATED_PATTERN,

            /// <summary>
            /// A special regular-expression character was used in the pattern that this
            /// implementation does not support.
            /// </summary>
            UNSUPPORTED_SPECIAL_CHARACTER,

            /// <summary>
            /// An escape-character construct was used in the pattern that this implementation
            /// does not support.
            /// </summary>
            UNSUPPORTED_ESCAPE_CHARACTER,

            /// <summary>
            /// A repetition count of zero was specified, or the minimum exceeded the maximum.
            /// </summary>
            INVALID_REPEAT_COUNT
        }

        private readonly ErrorType _type = type;
        private readonly int _position = pos;
        private readonly string _pattern = pattern;

        /// <summary>
        /// Gets the formatted error message.
        /// </summary>
        public override string Message => GetMessage();

        /// <summary>
        /// Returns the formatted error message, including the offending substring and its
        /// position.
        /// </summary>
        /// <returns>The formatted error message.</returns>
        public string GetMessage()
        {
            StringBuilder buffer = new();

            // Append error type name
            _ = buffer.Append(_type switch
            {
                ErrorType.UNEXPECTED_CHARACTER => "unexpected character",
                ErrorType.UNTERMINATED_PATTERN => "unterminated pattern",
                ErrorType.UNSUPPORTED_SPECIAL_CHARACTER => "unsupported character",
                ErrorType.UNSUPPORTED_ESCAPE_CHARACTER => "unsupported escape character",
                ErrorType.INVALID_REPEAT_COUNT => "invalid repeat count",
                _ => "internal error",
            });

            // Append erroneous character
            _ = buffer.Append(": ");
            if (_position < _pattern.Length)
            {
                _ = buffer.Append('\'');
                _ = buffer.Append(_pattern.Substring(_position));
                _ = buffer.Append('\'');
            }
            else
            {
                _ = buffer.Append("<end of pattern>");
            }

            // Append position
            _ = buffer.Append(" at position ");
            _ = buffer.Append(_position);

            return buffer.ToString();
        }
    }
}
