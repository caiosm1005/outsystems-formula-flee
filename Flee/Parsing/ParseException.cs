using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A parse-time exception. Carries an error type, optional information string, optional
    /// detail list (used for expected-token enumerations), and source-location coordinates.
    /// </summary>
    /// <param name="type">The kind of parse error.</param>
    /// <param name="info">An optional informational message.</param>
    /// <param name="details">An optional list of expected-token descriptions.</param>
    /// <param name="line">The line at which the error occurred.</param>
    /// <param name="column">The column at which the error occurred.</param>
    public class ParseException(ParseException.ErrorType type,
                          string? info,
                          ArrayList? details,
                          int line,
                          int column) : Exception
    {
        /// <summary>
        /// Enumerates the kinds of parse errors that can be reported.
        /// </summary>
        public enum ErrorType
        {
            /// <summary>
            /// Used to signal an error caused by a bug in the parser or tokenizer code itself.
            /// </summary>
            INTERNAL,

            /// <summary>
            /// Used for stream I/O errors.
            /// </summary>
            IO,

            /// <summary>
            /// Raised when end of file is encountered where a valid token was expected.
            /// </summary>
            UNEXPECTED_EOF,

            /// <summary>
            /// Raised when a character is read that isn't handled by any token pattern.
            /// </summary>
            UNEXPECTED_CHAR,

            /// <summary>
            /// Raised when a different token is encountered than the parser expected.
            /// </summary>
            UNEXPECTED_TOKEN,

            /// <summary>
            /// Raised when a token pattern with an attached error message is matched.
            /// </summary>
            INVALID_TOKEN,

            /// <summary>
            /// Raised when an error is encountered during analysis. <see cref="Info"/>
            /// contains the analyzer-supplied error message.
            /// </summary>
            ANALYSIS
        }

        private readonly ArrayList? _details = details;

        /// <summary>
        /// Initializes a new <see cref="ParseException"/> without a details list.
        /// </summary>
        /// <param name="type">The kind of parse error.</param>
        /// <param name="info">An optional informational message.</param>
        /// <param name="line">The line at which the error occurred.</param>
        /// <param name="column">The column at which the error occurred.</param>
        public ParseException(ErrorType type,
                              string? info,
                              int line,
                              int column)
            : this(type, info, null, line, column)
        {
        }

        /// <summary>
        /// Gets the kind of parse error.
        /// </summary>
        public ErrorType Type { get; } = type;

        /// <summary>
        /// Returns the kind of parse error.
        /// </summary>
        /// <returns>The error type.</returns>
        public ErrorType GetErrorType()
        {
            return Type;
        }

        /// <summary>
        /// Gets the informational message attached to the error, if any.
        /// </summary>
        public string? Info { get; } = info;

        /// <summary>
        /// Returns the informational message attached to the error.
        /// </summary>
        /// <returns>The information string, or <see langword="null"/> when none is set.</returns>
        public string? GetInfo()
        {
            return Info;
        }

        /// <summary>
        /// Gets a copy of the details list. The list typically holds expected-token
        /// descriptions for <see cref="ErrorType.UNEXPECTED_TOKEN"/>.
        /// </summary>
        public ArrayList Details => new(_details!);

        /// <summary>
        /// Returns a copy of the details list.
        /// </summary>
        /// <returns>The details list.</returns>
        public ArrayList GetDetails()
        {
            return Details;
        }

        /// <summary>
        /// Gets the line at which the error occurred.
        /// </summary>
        public int Line { get; } = line;

        /// <summary>
        /// Returns the line at which the error occurred.
        /// </summary>
        /// <returns>The error line.</returns>
        public int GetLine()
        {
            return Line;
        }

        /// <summary>
        /// Gets the column at which the error occurred.
        /// </summary>
        public int Column { get; } = column;

        /// <summary>
        /// Returns the column at which the error occurred.
        /// </summary>
        /// <returns>The error column.</returns>
        public int GetColumn()
        {
            return Column;
        }

        /// <summary>
        /// Gets the full error message, including the source location when available.
        /// </summary>
        public override string Message
        {
            get
            {
                StringBuilder buffer = new();

                // Add error description
                _ = buffer.Append(ErrorMessage);

                // Add line and column
                if (Line > 0 && Column > 0)
                {
                    _ = buffer.Append(", on line: ");
                    _ = buffer.Append(Line);
                    _ = buffer.Append(" column: ");
                    _ = buffer.Append(Column);
                }

                return buffer.ToString();
            }
        }

        /// <summary>
        /// Returns the full error message.
        /// </summary>
        /// <returns>The full error message.</returns>
        public string GetMessage()
        {
            return Message;
        }

        /// <summary>
        /// Gets the error description without source-location coordinates.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                StringBuilder buffer = new();

                // Add type and info
                switch (Type)
                {
                    case ErrorType.IO:
                        _ = buffer.Append("I/O error: ");
                        _ = buffer.Append(Info);
                        break;
                    case ErrorType.UNEXPECTED_EOF:
                        _ = buffer.Append("unexpected end of file");
                        break;
                    case ErrorType.UNEXPECTED_CHAR:
                        _ = buffer.Append("unexpected character '");
                        _ = buffer.Append(Info);
                        _ = buffer.Append("'");
                        break;
                    case ErrorType.UNEXPECTED_TOKEN:
                        _ = buffer.Append("unexpected token ");
                        _ = buffer.Append(Info);
                        if (_details != null)
                        {
                            _ = buffer.Append(", expected ");
                            if (_details.Count > 1)
                            {
                                _ = buffer.Append("one of ");
                            }
                            _ = buffer.Append(GetMessageDetails());
                        }
                        break;
                    case ErrorType.INVALID_TOKEN:
                        _ = buffer.Append(Info);
                        break;
                    case ErrorType.ANALYSIS:
                        _ = buffer.Append(Info);
                        break;
                    case ErrorType.INTERNAL:
                        break;
                    default:
                        _ = buffer.Append("internal error");
                        if (Info != null)
                        {
                            _ = buffer.Append(": ");
                            _ = buffer.Append(Info);
                        }
                        break;
                }

                return buffer.ToString();
            }
        }

        /// <summary>
        /// Returns the error description without source-location coordinates.
        /// </summary>
        /// <returns>The error message body.</returns>
        public string GetErrorMessage()
        {
            return ErrorMessage;
        }

        private string GetMessageDetails()
        {
            StringBuilder buffer = new();

            for (int i = 0; i < _details!.Count; i++)
            {
                if (i > 0)
                {
                    _ = buffer.Append(", ");
                    if (i + 1 == _details.Count)
                    {
                        _ = buffer.Append("or ");
                    }
                }
                _ = buffer.Append(_details[i]);
            }

            return buffer.ToString();
        }
    }
}
