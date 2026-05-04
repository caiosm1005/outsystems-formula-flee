using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A token pattern. Holds the definition of a single token (its name, type, and pattern
    /// source) and the optional error/ignore behavior associated with it. Each pattern is
    /// uniquely identified by an integer id supplied at construction time.
    /// </summary>
    /// <param name="id">The token id.</param>
    /// <param name="name">The token name.</param>
    /// <param name="type">The token type.</param>
    /// <param name="pattern">The pattern source (literal string or regex).</param>
    internal class TokenPattern(int id,
                        string name,
                        TokenPattern.PatternType type,
                        string pattern)
    {
        /// <summary>
        /// Enumerates the kinds of token patterns.
        /// </summary>
        public enum PatternType
        {
            /// <summary>
            /// The pattern matches an exact literal string.
            /// </summary>
            STRING,

            /// <summary>
            /// The pattern is a regular expression.
            /// </summary>
            REGEXP
        }

        private bool _error;
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Gets or sets the token id.
        /// </summary>
        public int Id { get; set; } = id;

        /// <summary>
        /// Returns the token id.
        /// </summary>
        /// <returns>The token id.</returns>
        public int GetId()
        {
            return Id;
        }

        /// <summary>
        /// Gets or sets the token name.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// Returns the token name.
        /// </summary>
        /// <returns>The token name.</returns>
        public string GetName()
        {
            return Name;
        }

        /// <summary>
        /// Gets or sets the token type.
        /// </summary>
        public PatternType Type { get; set; } = type;

        /// <summary>
        /// Returns the token type.
        /// </summary>
        /// <returns>The token type.</returns>
        public PatternType GetPatternType()
        {
            return Type;
        }

        /// <summary>
        /// Gets or sets the pattern source.
        /// </summary>
        public string Pattern { get; set; } = pattern;

        /// <summary>
        /// Returns the pattern source.
        /// </summary>
        /// <returns>The pattern source.</returns>
        public string GetPattern()
        {
            return Pattern;
        }

        /// <summary>
        /// Gets or sets whether matching this pattern signals an error. Setting this to
        /// <see langword="true"/> initializes <see cref="ErrorMessage"/> to a default when
        /// none has been set.
        /// </summary>
        public bool Error
        {
            get => _error;
            set
            {
                _error = value;
                if (_error && _errorMessage == null)
                {
                    _errorMessage = "unrecognized token found";
                }
            }
        }

        /// <summary>
        /// Gets or sets the error message produced when this pattern is matched. Setting it
        /// also marks the pattern as an error pattern.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _error = true;
                _errorMessage = value;
            }
        }

        /// <summary>
        /// Returns whether matching this pattern signals an error.
        /// </summary>
        /// <returns><see langword="true"/> when the pattern is an error pattern.</returns>
        public bool IsError()
        {
            return Error;
        }

        /// <summary>
        /// Returns the error message produced when this pattern is matched.
        /// </summary>
        /// <returns>The error message.</returns>
        public string GetErrorMessage()
        {
            return ErrorMessage;
        }

        /// <summary>
        /// Marks this pattern as an error pattern with the default error message.
        /// </summary>
        public void SetError()
        {
            Error = true;
        }

        /// <summary>
        /// Marks this pattern as an error pattern with the supplied error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public void SetError(string message)
        {
            ErrorMessage = message;
        }

        /// <summary>
        /// Gets or sets whether matching this pattern should be silently discarded by the
        /// tokenizer.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// Gets or sets the ignore message attached to the pattern. Setting it also marks the
        /// pattern as ignored.
        /// </summary>
        public string IgnoreMessage
        {
            get; set
            {
                Ignore = true;
                field = value;
            }
        } = string.Empty;

        /// <summary>
        /// Returns whether matching this pattern should be silently discarded.
        /// </summary>
        /// <returns><see langword="true"/> when the pattern is ignored.</returns>
        public bool IsIgnore()
        {
            return Ignore;
        }

        /// <summary>
        /// Returns the ignore message attached to the pattern.
        /// </summary>
        /// <returns>The ignore message.</returns>
        public string GetIgnoreMessage()
        {
            return IgnoreMessage;
        }

        /// <summary>
        /// Marks this pattern as ignored.
        /// </summary>
        public void SetIgnore()
        {
            Ignore = true;
        }

        /// <summary>
        /// Marks this pattern as ignored with the supplied ignore message.
        /// </summary>
        /// <param name="message">The ignore message.</param>
        public void SetIgnore(string message)
        {
            IgnoreMessage = message;
        }

        /// <summary>
        /// Gets or sets the diagnostic-info string attached to the pattern by the tokenizer
        /// engine that compiled it.
        /// </summary>
        public string DebugInfo { get; set; } = string.Empty;

        /// <summary>
        /// Returns a verbose textual description of this pattern, including diagnostic info.
        /// </summary>
        /// <returns>The verbose description.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new();

            _ = buffer.Append(Name);
            _ = buffer.Append(" (");
            _ = buffer.Append(Id);
            _ = buffer.Append("): ");
            switch (Type)
            {
                case PatternType.STRING:
                    _ = buffer.Append("\"");
                    _ = buffer.Append(Pattern);
                    _ = buffer.Append("\"");
                    break;
                case PatternType.REGEXP:
                    _ = buffer.Append("<<");
                    _ = buffer.Append(Pattern);
                    _ = buffer.Append(">>");
                    break;
                default:
                    break;
            }
            if (_error)
            {
                _ = buffer.Append(" ERROR: \"");
                _ = buffer.Append(_errorMessage);
                _ = buffer.Append("\"");
            }
            if (Ignore)
            {
                _ = buffer.Append(" IGNORE");
                if (IgnoreMessage != null)
                {
                    _ = buffer.Append(": \"");
                    _ = buffer.Append(IgnoreMessage);
                    _ = buffer.Append("\"");
                }
            }
            if (DebugInfo != null)
            {
                _ = buffer.Append("\n  ");
                _ = buffer.Append(DebugInfo);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Returns a short textual description of this pattern suitable for inclusion in
        /// error messages.
        /// </summary>
        /// <returns>The short description.</returns>
        public string ToShortString()
        {
            StringBuilder buffer = new();
            int newline = Pattern.IndexOf('\n');

            if (Type == PatternType.STRING)
            {
                _ = buffer.Append("\"");
                if (newline >= 0)
                {
                    if (newline > 0 && Pattern[newline - 1] == '\r')
                    {
                        newline--;
                    }
                    _ = buffer.Append(Pattern.Substring(0, newline));
                    _ = buffer.Append("(...)");
                }
                else
                {
                    _ = buffer.Append(Pattern);
                }
                _ = buffer.Append("\"");
            }
            else
            {
                _ = buffer.Append("<");
                _ = buffer.Append(Name);
                _ = buffer.Append(">");
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Replaces every public field of this pattern in a single call. Used by
        /// <see cref="CustomTokenPattern"/> subclasses that need to rewrite the pattern after
        /// reading the configured options from the active expression context.
        /// </summary>
        /// <param name="id">The new token id.</param>
        /// <param name="name">The new token name.</param>
        /// <param name="type">The new token type.</param>
        /// <param name="pattern">The new pattern source.</param>
        public void SetData(int id, string name, PatternType type, string pattern)
        {
            Id = id;
            Name = name;
            Type = type;
            Pattern = pattern;
        }
    }
}
