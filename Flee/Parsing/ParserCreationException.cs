using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /**
     * A parser creation exception. This exception is used for signalling
     * an error in the token or production patterns, making it impossible
     * to create a working parser or tokenizer.
     */
    internal class ParserCreationException(ParserCreationException.ErrorType type,
                                   String? name,
                                   String? info,
                                   ArrayList? details) : Exception
    {

        /**
         * The error type enumeration.
         */
        public enum ErrorType
        {

            /**
             * The internal error type is only used to signal an
             * error that is a result of a bug in the parser or
             * tokenizer code.
             */
            INTERNAL,

            /**
             * The invalid parser error type is used when the parser
             * as such is invalid. This error is typically caused by
             * using a parser without any patterns.
             */
            INVALID_PARSER,

            /**
             * The invalid token error type is used when a token
             * pattern is erroneous. This error is typically caused
             * by an invalid pattern type or an erroneous regular
             * expression.
             */
            INVALID_TOKEN,

            /**
             * The invalid production error type is used when a
             * production pattern is erroneous. This error is
             * typically caused by referencing undeclared productions,
             * or violating some other production pattern constraint.
             */
            INVALID_PRODUCTION,

            /**
             * The infinite loop error type is used when an infinite
             * loop has been detected in the grammar. One of the
             * productions in the loop will be reported.
             */
            INFINITE_LOOP,

            /**
             * The inherent ambiguity error type is used when the set
             * of production patterns (i.e. the grammar) contains
             * ambiguities that cannot be resolved.
             */
            INHERENT_AMBIGUITY



        }

        private readonly ArrayList? _details = details;

        public ParserCreationException(ErrorType type,
                                       String? info)
            : this(type, null, info)
        {
        }

        public ParserCreationException(ErrorType type,
                                       String? name,
                                       String? info)
            : this(type, name, info, null)
        {
        }

        public ErrorType Type { get; } = type;

        public ErrorType GetErrorType()
        {
            return Type;
        }

        public string? Name { get; } = name;

        public string? GetName()
        {
            return Name;
        }

        public string? Info { get; } = info;

        public string? GetInfo()
        {
            return Info;
        }

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

        public string? GetDetails()
        {
            return Details;
        }

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

        public string GetMessage()
        {
            return Message;
        }
    }
}
