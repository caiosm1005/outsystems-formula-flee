using System.Collections;
using System.Globalization;
using System.Text;

namespace Flee.Parsing
{
    /**
     * A regular expression parser. The parser creates an NFA for the
     * regular expression having a single start and acceptance states.
   */
    internal class TokenRegExpParser
    {
        private readonly string _pattern;
        private readonly bool _ignoreCase;
        private int _pos;
        internal NFAState Start = new();
        internal NFAState End;
        private int _stateCount;
        private int _transitionCount;
        private int _epsilonCount;

        public TokenRegExpParser(string pattern) : this(pattern, false)
        {
        }

        public TokenRegExpParser(string pattern, bool ignoreCase)
        {
            _pattern = pattern;
            _ignoreCase = ignoreCase;
            _pos = 0;
            End = ParseExpr(Start);
            if (_pos < pattern.Length)
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UNEXPECTED_CHARACTER,
                    _pos,
                    pattern);
            }
        }

        public string GetDebugInfo()
        {
            if (_stateCount == 0)
            {
                UpdateStats(Start, []);
            }
            return _stateCount + " states, " +
                   _transitionCount + " transitions, " +
                   _epsilonCount + " epsilons";
        }

        private void UpdateStats(NFAState state, Hashtable visited)
        {
            if (!visited.ContainsKey(state))
            {
                visited.Add(state, state);
                _stateCount++;
                for (int i = 0; i < state.Outgoing.Length; i++)
                {
                    _transitionCount++;
                    if (state.Outgoing[i] is NFAEpsilonTransition)
                    {
                        _epsilonCount++;
                    }
                    UpdateStats(state.Outgoing[i].State, visited);
                }
            }
        }

        private NFAState ParseExpr(NFAState start)
        {
            NFAState end = new();
            do
            {
                if (PeekChar(0) == '|')
                {
                    _ = ReadChar('|');
                }
                NFAState subStart = new();
                var subEnd = ParseTerm(subStart);
                if (subStart.Incoming.Length == 0)
                {
                    subStart.MergeInto(start);
                }
                else
                {
                    _ = start.AddOut(new NFAEpsilonTransition(subStart));
                }
                if (subEnd.Outgoing.Length == 0 ||
                    (!end.HasTransitions() && PeekChar(0) != '|'))
                {
                    subEnd.MergeInto(end);
                }
                else
                {
                    _ = subEnd.AddOut(new NFAEpsilonTransition(end));
                }
            } while (PeekChar(0) == '|');
            return end;
        }

        private NFAState ParseTerm(NFAState start)
        {
            var end = ParseFact(start);
            while (true)
            {
                switch (PeekChar(0))
                {
                    case -1:
                    case ')':
                    case ']':
                    case '{':
                    case '}':
                    case '?':
                    case '+':
                    case '|':
                        return end;
                    default:
                        end = ParseFact(end);
                        break;
                }
            }
        }

        private NFAState ParseFact(NFAState start)
        {
            NFAState placeholder = new();

            var end = ParseAtom(placeholder);
            switch (PeekChar(0))
            {
                case '?':
                case '*':
                case '+':
                case '{':
                    end = ParseAtomModifier(placeholder, end);
                    break;
                default:
                    break;
            }
            if (placeholder.Incoming.Length > 0 && start.Outgoing.Length > 0)
            {
                _ = start.AddOut(new NFAEpsilonTransition(placeholder));
                return end;
            }
            else
            {
                placeholder.MergeInto(start);
                return (end == placeholder) ? start : end;
            }
        }

        private NFAState ParseAtom(NFAState start)
        {
            NFAState end;

            switch (PeekChar(0))
            {
                case '.':
                    _ = ReadChar('.');
                    return start.AddOut(new NFADotTransition(new NFAState()));
                case '(':
                    _ = ReadChar('(');
                    end = ParseExpr(start);
                    _ = ReadChar(')');
                    return end;
                case '[':
                    _ = ReadChar('[');
                    end = ParseCharSet(start);
                    _ = ReadChar(']');
                    return end;
                case -1:
                case ')':
                case ']':
                case '{':
                case '}':
                case '?':
                case '*':
                case '+':
                case '|':
                    throw new RegExpException(
                        RegExpException.ErrorType.UNEXPECTED_CHARACTER,
                        _pos,
                        _pattern);
                default:
                    return ParseChar(start);
            }
        }

        private NFAState ParseAtomModifier(NFAState start, NFAState end)
        {
            int min;
            int max;
            int firstPos = _pos;

            // Read min and max
            switch (ReadChar())
            {
                case '?':
                    min = 0;
                    max = 1;
                    break;
                case '*':
                    min = 0;
                    max = -1;
                    break;
                case '+':
                    min = 1;
                    max = -1;
                    break;
                case '{':
                    min = ReadNumber();
                    max = min;
                    if (PeekChar(0) == ',')
                    {
                        _ = ReadChar(',');
                        max = -1;
                        if (PeekChar(0) != '}')
                        {
                            max = ReadNumber();
                        }
                    }
                    _ = ReadChar('}');
                    if (max == 0 || (max > 0 && min > max))
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.INVALID_REPEAT_COUNT,
                            firstPos,
                            _pattern);
                    }
                    break;
                default:
                    throw new RegExpException(
                        RegExpException.ErrorType.UNEXPECTED_CHARACTER,
                        _pos - 1,
                        _pattern);
            }

            // Read possessive or reluctant modifiers
            if (PeekChar(0) == '?')
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UNSUPPORTED_SPECIAL_CHARACTER,
                    _pos,
                    _pattern);
            }
            else if (PeekChar(0) == '+')
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UNSUPPORTED_SPECIAL_CHARACTER,
                    _pos,
                    _pattern);
            }

            // Handle supported repeaters
            if (min == 0 && max == 1)
            {
                return start.AddOut(new NFAEpsilonTransition(end));
            }
            else if (min == 0 && max == -1)
            {
                if (end.Outgoing.Length == 0)
                {
                    end.MergeInto(start);
                }
                else
                {
                    _ = end.AddOut(new NFAEpsilonTransition(start));
                }
                return start;
            }
            else if (min == 1 && max == -1)
            {
                _ = end.AddOut(
                    start.Outgoing.Length == 1 &&
                    end.Outgoing.Length == 0 &&
                    end.Incoming.Length == 1 &&
                    start.Outgoing[0] == end.Incoming[0]
                        ? start.Outgoing[0].Copy(end)
                        : new NFAEpsilonTransition(start));
                return end;
            }
            else
            {
                throw new RegExpException(
                    RegExpException.ErrorType.INVALID_REPEAT_COUNT,
                    firstPos,
                    _pattern);
            }
        }

        private NFAState ParseCharSet(NFAState start)
        {
            NFAState end = new();
            NFACharRangeTransition range;

            if (PeekChar(0) == '^')
            {
                _ = ReadChar('^');
                range = new NFACharRangeTransition(true, _ignoreCase, end);
            }
            else
            {
                range = new NFACharRangeTransition(false, _ignoreCase, end);
            }
            _ = start.AddOut(range);
            while (PeekChar(0) > 0)
            {
                var min = (char)PeekChar(0);
                switch (min)
                {
                    case ']':
                        return end;
                    case '\\':
                        range.AddCharacter(ReadEscapeChar());
                        break;
                    default:
                        _ = ReadChar(min);
                        if (PeekChar(0) == '-' &&
                            PeekChar(1) > 0 &&
                            PeekChar(1) != ']')
                        {

                            _ = ReadChar('-');
                            var max = ReadChar();
                            range.AddRange(min, max);
                        }
                        else
                        {
                            range.AddCharacter(min);
                        }
                        break;
                }
            }
            return end;
        }

        private NFAState ParseChar(NFAState start)
        {
            return PeekChar(0) switch
            {
                '\\' => ParseEscapeChar(start),
                '^' or '$' => throw new RegExpException(
                                        RegExpException.ErrorType.UNSUPPORTED_SPECIAL_CHARACTER,
                                        _pos,
                                        _pattern),
                _ => start.AddOut(ReadChar(), _ignoreCase, new NFAState()),
            };
        }

        private NFAState ParseEscapeChar(NFAState start)
        {
            NFAState end = new();

            if (PeekChar(0) == '\\' && PeekChar(1) > 0)
            {
                switch ((char)PeekChar(1))
                {
                    case 'd':
                        _ = ReadChar();
                        _ = ReadChar();
                        return start.AddOut(new NFADigitTransition(end));
                    case 'D':
                        _ = ReadChar();
                        _ = ReadChar();
                        return start.AddOut(new NFANonDigitTransition(end));
                    case 's':
                        _ = ReadChar();
                        _ = ReadChar();
                        return start.AddOut(new NFAWhitespaceTransition(end));
                    case 'S':
                        _ = ReadChar();
                        _ = ReadChar();
                        return start.AddOut(new NFANonWhitespaceTransition(end));
                    case 'w':
                        _ = ReadChar();
                        _ = ReadChar();
                        return start.AddOut(new NFAWordTransition(end));
                    case 'W':
                        _ = ReadChar();
                        _ = ReadChar();
                        return start.AddOut(new NFANonWordTransition(end));
                    default:
                        break;
                }
            }
            return start.AddOut(ReadEscapeChar(), _ignoreCase, end);
        }

        private char ReadEscapeChar()
        {
            string str;
            int value;

            _ = ReadChar('\\');
            var c = ReadChar();
            switch (c)
            {
                case '0':
                    c = ReadChar();
                    if (c is < '0' or > '3')
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UNSUPPORTED_ESCAPE_CHARACTER,
                            _pos - 3,
                            _pattern);
                    }
                    value = c - '0';
                    c = (char)PeekChar(0);
                    if (c is >= '0' and <= '7')
                    {
                        value *= 8;
                        value += ReadChar() - '0';
                        c = (char)PeekChar(0);
                        if (c is >= '0' and <= '7')
                        {
                            value *= 8;
                            value += ReadChar() - '0';
                        }
                    }
                    return (char)value;
                case 'x':
                    str = ReadChar().ToString() + ReadChar().ToString();
                    try
                    {
                        value = Int32.Parse(str, NumberStyles.AllowHexSpecifier);
                        return (char)value;
                    }
                    catch (FormatException)
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UNSUPPORTED_ESCAPE_CHARACTER,
                            _pos - str.Length - 2,
                            _pattern);
                    }
                case 'u':
                    str = ReadChar().ToString() +
                          ReadChar().ToString() +
                          ReadChar().ToString() +
                          ReadChar().ToString();
                    try
                    {
                        value = Int32.Parse(str, NumberStyles.AllowHexSpecifier);
                        return (char)value;
                    }
                    catch (FormatException)
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UNSUPPORTED_ESCAPE_CHARACTER,
                            _pos - str.Length - 2,
                            _pattern);
                    }
                case 't':
                    return '\t';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 'f':
                    return '\f';
                case 'a':
                    return '\u0007';
                case 'e':
                    return '\u001B';
                default:
                    if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
                    {
                        throw new RegExpException(
                            RegExpException.ErrorType.UNSUPPORTED_ESCAPE_CHARACTER,
                            _pos - 2,
                            _pattern);
                    }
                    return c;
            }
        }

        private int ReadNumber()
        {
            StringBuilder buf = new();
            int c;

            c = PeekChar(0);
            while (c is >= '0' and <= '9')
            {
                _ = buf.Append(ReadChar());
                c = PeekChar(0);
            }
            return buf.Length <= 0
                ? throw new RegExpException(
                    RegExpException.ErrorType.UNEXPECTED_CHARACTER,
                    _pos,
                    _pattern)
                : Int32.Parse(buf.ToString());
        }

        private char ReadChar()
        {
            int c = PeekChar(0);

            if (c < 0)
            {
                throw new RegExpException(
                    RegExpException.ErrorType.UNTERMINATED_PATTERN,
                    _pos,
                    _pattern);
            }
            else
            {
                _pos++;
                return (char)c;
            }
        }

        private char ReadChar(char c)
        {
            return c != ReadChar()
                ? throw new RegExpException(
                    RegExpException.ErrorType.UNEXPECTED_CHARACTER,
                    _pos - 1,
                    _pattern)
                : c;
        }

        private int PeekChar(int count)
        {
            return _pos + count < _pattern.Length ? _pattern[_pos + count] : -1;
        }
    }
}
