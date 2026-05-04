using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A regular-expression character-set element. Matches a single character that is inside
    /// (or, when inverted, outside) the character set. Sets may include literal characters,
    /// character ranges, and other nested character sets.
    /// </summary>
    /// <param name="inverted">Whether the set's membership test should be inverted.</param>
    internal class CharacterSetElement(bool inverted) : Element
    {
        /// <summary>
        /// Predefined character set matching the regex dot (any character except line terminators).
        /// </summary>
        public static CharacterSetElement Dot = new(false);

        /// <summary>
        /// Predefined character set matching <c>\d</c> (decimal digits).
        /// </summary>
        public static CharacterSetElement Digit = new(false);

        /// <summary>
        /// Predefined character set matching <c>\D</c> (non-digits).
        /// </summary>
        public static CharacterSetElement NonDigit = new(true);

        /// <summary>
        /// Predefined character set matching <c>\s</c> (whitespace).
        /// </summary>
        public static CharacterSetElement Whitespace = new(false);

        /// <summary>
        /// Predefined character set matching <c>\S</c> (non-whitespace).
        /// </summary>
        public static CharacterSetElement NonWhitespace = new(true);

        /// <summary>
        /// Predefined character set matching <c>\w</c> (word characters).
        /// </summary>
        public static CharacterSetElement Word = new(false);

        /// <summary>
        /// Predefined character set matching <c>\W</c> (non-word characters).
        /// </summary>
        public static CharacterSetElement NonWord = new(true);

        private readonly bool _inverted = inverted;
        private readonly ArrayList _contents = [];

        /// <summary>
        /// Adds a single literal character to the set.
        /// </summary>
        /// <param name="c">The character to add.</param>
        public void AddCharacter(char c)
        {
            _ = _contents.Add(c);
        }

        /// <summary>
        /// Adds every character of <paramref name="str"/> to the set.
        /// </summary>
        /// <param name="str">The characters to add.</param>
        public void AddCharacters(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                AddCharacter(str[i]);
            }
        }

        /// <summary>
        /// Adds every character of <paramref name="elem"/> to the set.
        /// </summary>
        /// <param name="elem">The string element whose characters should be added.</param>
        public void AddCharacters(StringElement elem)
        {
            AddCharacters(elem.GetString());
        }

        /// <summary>
        /// Adds an inclusive character range to the set.
        /// </summary>
        /// <param name="min">The lowest character in the range.</param>
        /// <param name="max">The highest character in the range.</param>
        public void AddRange(char min, char max)
        {
            _ = _contents.Add(new Range(min, max));
        }

        /// <summary>
        /// Nests another character set inside this one.
        /// </summary>
        /// <param name="elem">The character set to add.</param>
        public void AddCharacterSet(CharacterSetElement elem)
        {
            _ = _contents.Add(elem);
        }

        /// <summary>
        /// Returns this element unchanged. Character sets are treated as immutable, so a shared
        /// reference is sufficient.
        /// </summary>
        /// <returns>This instance.</returns>
        public override object Clone()
        {
            return this;
        }

        /// <summary>
        /// Tests whether the next character at <paramref name="start"/> belongs to the set.
        /// </summary>
        /// <param name="m">The matcher tracking case sensitivity and end-of-stream state.</param>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <param name="start">The starting position in the buffer.</param>
        /// <param name="skip">The number of matches to skip before returning one.</param>
        /// <returns><c>1</c> when a character was matched; <c>-1</c> otherwise.</returns>
        public override int Match(Matcher m,
                                  ReaderBuffer buffer,
                                  int start,
                                  int skip)
        {

            int c;

            if (skip != 0)
            {
                return -1;
            }
            c = buffer.Peek(start);
            if (c < 0)
            {
                m.SetReadEndOfString();
                return -1;
            }
            if (m.IsCaseInsensitive())
            {
                c = Char.ToLower((char)c);
            }
            return InSet((char)c) ? 1 : -1;
        }

        private bool InSet(char c)
        {
            return this == Dot
                ? InDotSet(c)
                : this == Digit || this == NonDigit
                    ? InDigitSet(c) != _inverted
                    : this == Whitespace || this == NonWhitespace
                        ? InWhitespaceSet(c) != _inverted
                        : this == Word || this == NonWord
                            ? InWordSet(c) != _inverted
                            : InUserSet(c) != _inverted;
        }

        private bool InDotSet(char c)
        {
            return c switch
            {
                '\n' or '\r' or '\u0085' or '\u2028' or '\u2029' => false,
                _ => true,
            };
        }

        private bool InDigitSet(char c)
        {
            return c is >= '0' and <= '9';
        }

        private bool InWhitespaceSet(char c)
        {
            return c switch
            {
                ' ' or '\t' or '\n' or '\f' or '\r' or (char)11 => true,
                _ => false,
            };
        }

        private bool InWordSet(char c)
        {
            return c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';
        }

        private bool InUserSet(char value)
        {
            for (int i = 0; i < _contents.Count; i++)
            {
                var obj = _contents[i];
                if (obj is char c)
                {
                    if (c == value)
                    {
                        return true;
                    }
                }
                else if (obj is Range r)
                {
                    if (r.Inside(value))
                    {
                        return true;
                    }
                }
                else if (obj is CharacterSetElement e)
                {
                    if (e.InSet(value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Writes a textual description of this element to <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The text writer that receives the description.</param>
        /// <param name="indent">The indentation prefix to apply to the line.</param>
        public override void PrintTo(TextWriter output, string indent)
        {
            output.WriteLine(indent + ToString());
        }

        /// <summary>
        /// Returns a regex-style string representation of this character set, e.g. <c>\d</c>
        /// or <c>[a-z0-9]</c>.
        /// </summary>
        /// <returns>The regex-style description.</returns>
        public override string ToString()
        {
            // Handle predefined character sets
            if (this == Dot)
            {
                return ".";
            }
            else if (this == Digit)
            {
                return "\\d";
            }
            else if (this == NonDigit)
            {
                return "\\D";
            }
            else if (this == Whitespace)
            {
                return "\\s";
            }
            else if (this == NonWhitespace)
            {
                return "\\S";
            }
            else if (this == Word)
            {
                return "\\w";
            }
            else if (this == NonWord)
            {
                return "\\W";
            }

            // Handle user-defined character sets
            StringBuilder buffer = new();
            _ = buffer.Append(_inverted ? "^[" : "[");
            for (int i = 0; i < _contents.Count; i++)
            {
                _ = buffer.Append(_contents[i]);
            }
            _ = buffer.Append("]");

            return buffer.ToString();
        }

        private class Range(char min, char max)
        {
            private readonly char _min = min;
            private readonly char _max = max;

            public bool Inside(char c)
            {
                return _min <= c && c <= _max;
            }

            public override string ToString()
            {
                return _min + "-" + _max;
            }
        }
    }
}
