using System.Text;


namespace Flee.Parsing
{
    /**
     * A regular expression exception. This exception is thrown if a
     * regular expression couldn't be processed (or "compiled")
     * properly.
     */
    internal class RegExpException(RegExpException.ErrorType type, int pos, string pattern) : Exception
    {
        public enum ErrorType
        {

            /**
             * The unexpected character error constant. This error is
             * used when a character was read that didn't match the
             * allowed set of characters at the given position.
             */
            UNEXPECTED_CHARACTER,

            /**
             * The unterminated pattern error constant. This error is
             * used when more characters were expected in the pattern.
             */
            UNTERMINATED_PATTERN,

            /**
             * The unsupported special character error constant. This
             * error is used when special regular expression
             * characters are used in the pattern, but not supported
             * in this implementation.
             */
            UNSUPPORTED_SPECIAL_CHARACTER,

            /**
             * The unsupported escape character error constant. This
             * error is used when an escape character construct is
             * used in the pattern, but not supported in this
             * implementation.
             */
            UNSUPPORTED_ESCAPE_CHARACTER,

            /**
             * The invalid repeat count error constant. This error is
             * used when a repetition count of zero is specified, or
             * when the minimum exceeds the maximum.
             */
            INVALID_REPEAT_COUNT
        }

        private readonly ErrorType _type = type;
        private readonly int _position = pos;
        private readonly string _pattern = pattern;

        public override string Message => GetMessage();

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
