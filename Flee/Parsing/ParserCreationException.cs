using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// Signals an error in the token or production patterns that prevents creating a working
    /// parser or tokenizer.
    /// </summary>
    /// <param name="type">The kind of creation error.</param>
    /// <param name="name">The pattern name involved in the error, if any.</param>
    /// <param name="info">An optional informational message.</param>
    /// <param name="details">An optional list of detail strings.</param>
    internal class ParserCreationException(ParserCreationException.ErrorType type,
                                   String? name,
                                   String? info,
                                   ArrayList? details) : Exception
    {
        /// <summary>
        /// Enumerates the kinds of parser-creation errors.
        /// </summary>
        public enum ErrorType
        {
            /// <summary>
            /// Used to signal an error caused by a bug in the parser or tokenizer code itself.
            /// </summary>
            INTERNAL,

            /// <summary>
            /// Used when the parser as a whole is invalid, typically because no patterns have
            /// been registered.
            /// </summary>
            INVALID_PARSER,

            /// <summary>
            /// Used when a token pattern is malformed (e.g. an unsupported pattern type or an
            /// invalid regular expression).
            /// </summary>
            INVALID_TOKEN,

            /// <summary>
            /// Used when a production pattern is malformed (e.g. references an undeclared
            /// production or violates another constraint).
            /// </summary>
            INVALID_PRODUCTION,

            /// <summary>
            /// Used when an infinite loop is detected in the grammar; one of the productions
            /// in the loop is reported.
            /// </summary>
            INFINITE_LOOP,

            /// <summary>
            /// Used when the grammar contains ambiguities that cannot be resolved.
            /// </summary>
            INHERENT_AMBIGUITY
        }

        private readonly ArrayList? _details = details;

        /// <summary>
        /// Initializes a new <see cref="ParserCreationException"/> with no name and no details.
        /// </summary>
        /// <param name="type">The kind of creation error.</param>
        /// <param name="info">An optional informational message.</param>
        public ParserCreationException(ErrorType type,
                                       String? info)
            : this(type, null, info)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ParserCreationException"/> with no details.
        /// </summary>
        /// <param name="type">The kind of creation error.</param>
        /// <param name="name">The pattern name involved in the error, if any.</param>
        /// <param name="info">An optional informational message.</param>
        public ParserCreationException(ErrorType type,
                                       String? name,
                                       String? info)
            : this(type, name, info, null)
        {
        }

        /// <summary>
        /// Gets the kind of creation error.
        /// </summary>
        public ErrorType Type { get; } = type;

        /// <summary>
        /// Returns the kind of creation error.
        /// </summary>
        /// <returns>The error type.</returns>
        public ErrorType GetErrorType()
        {
            return Type;
        }

        /// <summary>
        /// Gets the pattern name involved in the error, if any.
        /// </summary>
        public string? Name { get; } = name;

        /// <summary>
        /// Returns the pattern name involved in the error.
        /// </summary>
        /// <returns>The pattern name, or <see langword="null"/> when none is set.</returns>
        public string? GetName()
        {
            return Name;
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
        /// Gets the formatted details string, or <see langword="null"/> when no details were
        /// attached.
        /// </summary>
        public string? Details
        {
            get
            {
                StringBuilder buffer = new();

                if (_details == null)
                {
                    return null;
                }
                for (int i = 0; i < _details.Count; i++)
                {
                    if (i > 0)
                    {
                        _ = buffer.Append(", ");
                        if (i + 1 == _details.Count)
                        {
                            _ = buffer.Append("and ");
                        }
                    }
                    _ = buffer.Append(_details[i]);
                }

                return buffer.ToString();
            }
        }

        /// <summary>
        /// Returns the formatted details string.
        /// </summary>
        /// <returns>The details string, or <see langword="null"/> when none was attached.</returns>
        public string? GetDetails()
        {
            return Details;
        }

        /// <summary>
        /// Gets the full error message.
        /// </summary>
        public override string Message
        {
            get
            {
                StringBuilder buffer = new();

                switch (Type)
                {
                    case ErrorType.INVALID_PARSER:
                        _ = buffer.Append("parser is invalid, as ");
                        _ = buffer.Append(Info);
                        break;
                    case ErrorType.INVALID_TOKEN:
                        _ = buffer.Append("token '");
                        _ = buffer.Append(Name);
                        _ = buffer.Append("' is invalid, as ");
                        _ = buffer.Append(Info);
                        break;
                    case ErrorType.INVALID_PRODUCTION:
                        _ = buffer.Append("production '");
                        _ = buffer.Append(Name);
                        _ = buffer.Append("' is invalid, as ");
                        _ = buffer.Append(Info);
                        break;
                    case ErrorType.INFINITE_LOOP:
                        _ = buffer.Append("infinite loop found in production pattern '");
                        _ = buffer.Append(Name);
                        _ = buffer.Append("'");
                        break;
                    case ErrorType.INHERENT_AMBIGUITY:
                        _ = buffer.Append("inherent ambiguity in production '");
                        _ = buffer.Append(Name);
                        _ = buffer.Append("'");
                        if (Info != null)
                        {
                            _ = buffer.Append(" ");
                            _ = buffer.Append(Info);
                        }
                        if (_details != null)
                        {
                            _ = buffer.Append(" starting with ");
                            _ = buffer.Append(_details.Count > 1 ? "tokens " : "token ");
                            _ = buffer.Append(Details);
                        }
                        break;
                    case ErrorType.INTERNAL:
                        break;
                    default:
                        _ = buffer.Append("internal error");
                        break;
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
    }
}
