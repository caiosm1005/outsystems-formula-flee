using System.Text;

namespace Flee.Parsing
{
    /**
     * A token pattern. This class contains the definition of a token
     * (i.e. it's pattern), and allows testing a string against this
     * pattern. A token pattern is uniquely identified by an integer id,
     * that must be provided upon creation.
     *
    
     */
    internal class TokenPattern(int id,
                        string name,
TokenPattern.PatternType type,
                        string pattern)
    {
        public enum PatternType
        {

            /**
             * The string pattern type is used for tokens that only
             * match an exact string.
             */
            STRING,

            /**
             * The regular expression pattern type is used for tokens
             * that match a regular expression.
             */
            REGEXP
        }

        private bool _error;
        private string _errorMessage = string.Empty;

        public int Id { get; set; } = id;

        public int GetId()
        {
            return Id;
        }

        public string Name { get; set; } = name;

        public string GetName()
        {
            return Name;
        }

        public PatternType Type { get; set; } = type;

        public PatternType GetPatternType()
        {
            return Type;
        }

        public string Pattern { get; set; } = pattern;

        public string GetPattern()
        {
            return Pattern;
        }

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

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _error = true;
                _errorMessage = value;
            }
        }

        public bool IsError()
        {
            return Error;
        }

        public string GetErrorMessage()
        {
            return ErrorMessage;
        }

        public void SetError()
        {
            Error = true;
        }

        public void SetError(string message)
        {
            ErrorMessage = message;
        }

        public bool Ignore { get; set; }

        public string IgnoreMessage
        {
            get; set
            {
                Ignore = true;
                field = value;
            }
        } = string.Empty;

        public bool IsIgnore()
        {
            return Ignore;
        }

        public string GetIgnoreMessage()
        {
            return IgnoreMessage;
        }


        public void SetIgnore()
        {
            Ignore = true;
        }


        public void SetIgnore(string message)
        {
            IgnoreMessage = message;
        }

        public string DebugInfo { get; set; } = string.Empty;

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

        public void SetData(int id, string name, PatternType type, string pattern)
        {
            Id = id;
            Name = name;
            Type = type;
            Pattern = pattern;
        }
    }
}
