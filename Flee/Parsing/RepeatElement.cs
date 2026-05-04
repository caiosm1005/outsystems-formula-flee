using System.Collections;

namespace Flee.Parsing
{
    /// <summary>
    /// A regular-expression element repeater. Matches the wrapped element repeatedly,
    /// honoring the configured minimum and maximum counts and the chosen
    /// <see cref="RepeatType"/>.
    /// </summary>
    /// <param name="elem">The element to repeat.</param>
    /// <param name="min">The minimum number of repetitions.</param>
    /// <param name="max">The maximum number of repetitions, or zero/negative for unbounded.</param>
    /// <param name="type">The repetition strategy.</param>
    internal class RepeatElement(
        Element elem,
        int min,
        int max,
        RepeatElement.RepeatType type) : Element
    {
        /// <summary>
        /// Enumerates the supported repetition strategies.
        /// </summary>
        public enum RepeatType
        {
            /// <summary>
            /// Match as many repetitions as possible, then back off to satisfy the rest of the
            /// expression.
            /// </summary>
            GREEDY = 1,

            /// <summary>
            /// Match as few repetitions as possible, then advance.
            /// </summary>
            RELUCTANT = 2,

            /// <summary>
            /// Match as many repetitions as possible without backing off.
            /// </summary>
            POSSESSIVE = 3
        }

        private readonly Element _elem = elem;
        private readonly int _min = min;
        private readonly int _max = max <= 0 ? int.MaxValue : max;
        private readonly RepeatType _type = type;
        private int _matchStart = -1;
        private BitArray? _matches = null;

        /// <summary>
        /// Returns a deep clone wrapping a clone of the underlying element.
        /// </summary>
        /// <returns>The cloned repeater.</returns>
        public override object Clone()
        {
            return new RepeatElement((Element)_elem.Clone(),
                                     _min,
                                     _max,
                                     _type);
        }

        /// <summary>
        /// Matches the wrapped element repeatedly.
        /// </summary>
        /// <param name="m">The matcher tracking case sensitivity and end-of-stream state.</param>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <param name="start">The starting position in the buffer.</param>
        /// <param name="skip">The number of matches to skip before returning one.</param>
        /// <returns>The match length, or <c>-1</c> if no match was found.</returns>
        public override int Match(Matcher m,
                                  ReaderBuffer buffer,
                                  int start,
                                  int skip)
        {
            if (skip == 0)
            {
                _matchStart = -1;
                _matches = null;
            }
            switch (_type)
            {
                case RepeatType.GREEDY:
                    return MatchGreedy(m, buffer, start, skip);
                case RepeatType.RELUCTANT:
                    return MatchReluctant(m, buffer, start, skip);
                case RepeatType.POSSESSIVE:
                    if (skip == 0)
                    {
                        return MatchPossessive(m, buffer, start, 0);
                    }
                    break;
                default:
                    break;
            }
            return -1;
        }

        private int MatchGreedy(Matcher m,
                                ReaderBuffer buffer,
                                int start,
                                int skip)
        {
            // Check for simple case
            if (skip == 0)
            {
                return MatchPossessive(m, buffer, start, 0);
            }

            // Find all matches
            if (_matchStart != start)
            {
                _matchStart = start;
                _matches = new BitArray(10);
                FindMatches(m, buffer, start, 0, 0, 0);
            }

            // Find first non-skipped match
            for (int i = _matches!.Count - 1; i >= 0; i--)
            {
                if (_matches[i])
                {
                    if (skip == 0)
                    {
                        return i;
                    }
                    skip--;
                }
            }
            return -1;
        }

        private int MatchReluctant(Matcher m,
                                   ReaderBuffer buffer,
                                   int start,
                                   int skip)
        {
            if (_matchStart != start)
            {
                _matchStart = start;
                _matches = new BitArray(10);
                FindMatches(m, buffer, start, 0, 0, 0);
            }

            // Find first non-skipped match
            for (int i = 0; i < _matches!.Count; i++)
            {
                if (_matches[i])
                {
                    if (skip == 0)
                    {
                        return i;
                    }
                    skip--;
                }
            }
            return -1;
        }

        private int MatchPossessive(Matcher m,
                                    ReaderBuffer buffer,
                                    int start,
                                    int count)
        {
            int length = 0;
            int subLength = 1;

            // Match as many elements as possible
            while (subLength > 0 && count < _max)
            {
                subLength = _elem.Match(m, buffer, start + length, 0);
                if (subLength >= 0)
                {
                    count++;
                    length += subLength;
                }
            }

            // Return result
            return _min <= count && count <= _max ? length : -1;
        }

        private void FindMatches(Matcher m,
                                 ReaderBuffer buffer,
                                 int start,
                                 int length,
                                 int count,
                                 int attempt)
        {
            int subLength;

            // Check match ending here
            if (count > _max)
            {
                return;
            }
            if (_min <= count && attempt == 0)
            {
                if (_matches!.Length <= length)
                {
                    _matches.Length = length + 10;
                }
                _matches[length] = true;
            }

            // Check element match
            subLength = _elem.Match(m, buffer, start, attempt);
            if (subLength < 0)
            {
                return;
            }
            else if (subLength == 0)
            {
                if (_min == count + 1)
                {
                    if (_matches!.Length <= length)
                    {
                        _matches.Length = length + 10;
                    }
                    _matches[length] = true;
                }
                return;
            }

            // Find alternative and subsequent matches
            FindMatches(m, buffer, start, length, count, attempt + 1);
            FindMatches(m,
                        buffer,
                        start + subLength,
                        length + subLength,
                        count + 1,
                        0);
        }

        /// <summary>
        /// Writes a textual description of this repeater and its inner element to
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The text writer that receives the description.</param>
        /// <param name="indent">The indentation prefix to apply to each line.</param>
        public override void PrintTo(TextWriter output, string indent)
        {
            output.Write(indent + "Repeat (" + _min + "," + _max + ")");
            if (_type == RepeatType.RELUCTANT)
            {
                output.Write("?");
            }
            else if (_type == RepeatType.POSSESSIVE)
            {
                output.Write("+");
            }
            output.WriteLine();
            _elem.PrintTo(output, indent + "  ");
        }
    }
}
