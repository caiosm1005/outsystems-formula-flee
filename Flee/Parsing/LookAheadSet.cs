using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A token look-ahead set. Holds a deduplicated set of token-id sequences whose length is
    /// bounded by a maximum. Each sequence carries a repeat flag so the set can describe
    /// possible infinite repetitions of a sequence; conflicts that involve repetitive
    /// sequences cannot be resolved (they imply an infinite loop).
    /// </summary>
    /// <param name="maxLength">The maximum length of any sequence stored in the set.</param>
    internal class LookAheadSet(int maxLength)
    {
        private readonly ArrayList _elements = [];
        private readonly int _maxLength = maxLength;

        /// <summary>
        /// Initializes a new look-ahead set as a copy of <paramref name="set"/>.
        /// </summary>
        /// <param name="maxLength">The maximum sequence length.</param>
        /// <param name="set">The source look-ahead set to copy from.</param>
        public LookAheadSet(int maxLength, LookAheadSet set)
            : this(maxLength)
        {

            AddAll(set);
        }

        /// <summary>
        /// Returns the number of sequences stored in the set.
        /// </summary>
        /// <returns>The sequence count.</returns>
        public int Size()
        {
            return _elements.Count;
        }

        /// <summary>
        /// Returns the length of the shortest sequence in the set.
        /// </summary>
        /// <returns>The minimum sequence length, or zero when the set is empty.</returns>
        public int GetMinLength()
        {
            int min = -1;

            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                if (min < 0 || seq.Length() < min)
                {
                    min = seq.Length();
                }
            }
            return (min < 0) ? 0 : min;
        }

        /// <summary>
        /// Returns the length of the longest sequence in the set.
        /// </summary>
        /// <returns>The maximum sequence length.</returns>
        public int GetMaxLength()
        {
            int max = 0;
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                if (seq.Length() > max)
                {
                    max = seq.Length();
                }
            }
            return max;
        }

        /// <summary>
        /// Returns the deduplicated list of token ids that appear at the start of any sequence
        /// in the set.
        /// </summary>
        /// <returns>The initial token ids.</returns>
        public int[] GetInitialTokens()
        {
            ArrayList list = [];
            int i;
            for (i = 0; i < _elements.Count; i++)
            {
                var token = ((Sequence)_elements[i]!).GetToken(0);
                if (token != null && !list.Contains(token))
                {
                    _ = list.Add(token);
                }
            }
            var result = new int[list.Count];
            for (i = 0; i < list.Count; i++)
            {
                result[i] = (int)list[i]!;
            }
            return result;
        }

        /// <summary>
        /// Returns whether any sequence in the set is marked as repetitive.
        /// </summary>
        /// <returns><see langword="true"/> when at least one sequence is repetitive.</returns>
        public bool IsRepetitive()
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                if (seq.IsRepetitive())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether any sequence in the set matches the upcoming tokens of
        /// <paramref name="parser"/>.
        /// </summary>
        /// <param name="parser">The parser to peek tokens from.</param>
        /// <returns><see langword="true"/> when at least one sequence matches.</returns>
        public bool IsNext(Parser parser)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                if (seq.IsNext(parser))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether any sequence in the set matches the upcoming tokens of
        /// <paramref name="parser"/>, considering only the first <paramref name="length"/>
        /// tokens.
        /// </summary>
        /// <param name="parser">The parser to peek tokens from.</param>
        /// <param name="length">The maximum number of tokens to consider.</param>
        /// <returns><see langword="true"/> when at least one sequence matches.</returns>
        public bool IsNext(Parser parser, int length)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                if (seq.IsNext(parser, length))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether any sequence in this set overlaps a sequence in
        /// <paramref name="set"/> (one is a prefix of the other).
        /// </summary>
        /// <param name="set">The other set.</param>
        /// <returns><see langword="true"/> when an overlap exists.</returns>
        public bool IsOverlap(LookAheadSet set)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                if (set.IsOverlap((Sequence)_elements[i]!))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsOverlap(Sequence seq)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence elem = (Sequence)_elements[i]!;
                if (seq.StartsWith(elem) || elem.StartsWith(seq))
                {
                    return true;
                }
            }
            return false;
        }

        private bool Contains(Sequence elem)
        {
            return FindSequence(elem) != null;
        }

        /// <summary>
        /// Returns whether this set and <paramref name="set"/> share at least one sequence.
        /// </summary>
        /// <param name="set">The other set.</param>
        /// <returns><see langword="true"/> when a shared sequence exists.</returns>
        public bool Intersects(LookAheadSet set)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                if (set.Contains((Sequence)_elements[i]!))
                {
                    return true;
                }
            }
            return false;
        }

        private Sequence? FindSequence(Sequence elem)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i]!.Equals(elem))
                {
                    return (Sequence)_elements[i]!;
                }
            }
            return null;
        }

        private void Add(Sequence seq)
        {
            if (seq.Length() > _maxLength)
            {
                seq = new Sequence(_maxLength, seq);
            }
            if (!Contains(seq))
            {
                _ = _elements.Add(seq);
            }
        }

        /// <summary>
        /// Adds a single-token sequence to the set.
        /// </summary>
        /// <param name="token">The token id to add.</param>
        public void Add(int token)
        {
            Add(new Sequence(token));
        }

        /// <summary>
        /// Adds every sequence from <paramref name="set"/> to this set.
        /// </summary>
        /// <param name="set">The set to merge in.</param>
        public void AddAll(LookAheadSet set)
        {
            for (int i = 0; i < set._elements.Count; i++)
            {
                Add((Sequence)set._elements[i]!);
            }
        }

        /// <summary>
        /// Adds the empty sequence to the set.
        /// </summary>
        public void AddEmpty()
        {
            Add(new Sequence());
        }

        private void Remove(Sequence seq)
        {
            _elements.Remove(seq);
        }

        /// <summary>
        /// Removes every sequence in <paramref name="set"/> from this set.
        /// </summary>
        /// <param name="set">The set whose sequences should be removed.</param>
        public void RemoveAll(LookAheadSet set)
        {
            for (int i = 0; i < set._elements.Count; i++)
            {
                Remove((Sequence)set._elements[i]!);
            }
        }

        /// <summary>
        /// Returns the look-ahead set obtained after consuming <paramref name="token"/> from
        /// the front of every matching sequence.
        /// </summary>
        /// <param name="token">The token id to consume.</param>
        /// <returns>The remaining-look-ahead set.</returns>
        public LookAheadSet CreateNextSet(int token)
        {
            LookAheadSet result = new(_maxLength - 1);
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                var value = seq.GetToken(0);
                if (value != null && token == (int)value)
                {
                    result.Add(seq.Subsequence(1));
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the intersection of this set and <paramref name="set"/>, preferring the
        /// repetitive sequence when both share one.
        /// </summary>
        /// <param name="set">The other set.</param>
        /// <returns>The intersection set.</returns>
        public LookAheadSet CreateIntersection(LookAheadSet set)
        {
            LookAheadSet result = new(_maxLength);
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq1 = (Sequence)_elements[i]!;
                var seq2 = set.FindSequence(seq1);
                if (seq2 != null && seq1.IsRepetitive())
                {
                    result.Add(seq2);
                }
                else if (seq2 != null)
                {
                    result.Add(seq1);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns every concatenation of a sequence in this set followed by a sequence in
        /// <paramref name="set"/>, truncated to the maximum length.
        /// </summary>
        /// <param name="set">The follow-up set.</param>
        /// <returns>The combined set.</returns>
        public LookAheadSet CreateCombination(LookAheadSet set)
        {
            LookAheadSet result = new(_maxLength);

            // Handle special cases
            if (Size() <= 0)
            {
                return set;
            }
            else if (set.Size() <= 0)
            {
                return this;
            }

            // Create combinations
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence first = (Sequence)_elements[i]!;
                if (first.Length() >= _maxLength)
                {
                    result.Add(first);
                }
                else if (first.Length() <= 0)
                {
                    result.AddAll(set);
                }
                else
                {
                    for (int j = 0; j < set._elements.Count; j++)
                    {
                        Sequence second = (Sequence)set._elements[j]!;
                        result.Add(first.Concat(_maxLength, second));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns every sequence in this set that overlaps a sequence in
        /// <paramref name="set"/>.
        /// </summary>
        /// <param name="set">The other set.</param>
        /// <returns>The overlapping sequences.</returns>
        public LookAheadSet CreateOverlaps(LookAheadSet set)
        {
            LookAheadSet result = new(_maxLength);

            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                if (set.IsOverlap(seq))
                {
                    result.Add(seq);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns every suffix produced by stripping a prefix in <paramref name="set"/> from
        /// the sequences in this set.
        /// </summary>
        /// <param name="set">The set of prefixes to filter against.</param>
        /// <returns>The filtered set.</returns>
        public LookAheadSet CreateFilter(LookAheadSet set)
        {
            LookAheadSet result = new(_maxLength);

            // Handle special cases
            if (Size() <= 0 || set.Size() <= 0)
            {
                return this;
            }

            // Create combinations
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence first = (Sequence)_elements[i]!;
                for (int j = 0; j < set._elements.Count; j++)
                {
                    Sequence second = (Sequence)set._elements[j]!;
                    if (first.StartsWith(second))
                    {
                        result.Add(first.Subsequence(second.Length()));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a copy of this set with every sequence flagged as repetitive.
        /// </summary>
        /// <returns>The repetitive copy.</returns>
        public LookAheadSet CreateRepetitive()
        {
            LookAheadSet result = new(_maxLength);

            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                if (seq.IsRepetitive())
                {
                    result.Add(seq);
                }
                else
                {
                    result.Add(new Sequence(true, seq));
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a string representation of the set.
        /// </summary>
        /// <returns>The string form.</returns>
        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// Returns a string representation of the set, using <paramref name="tokenizer"/> to
        /// resolve token-id descriptions when available.
        /// </summary>
        /// <param name="tokenizer">An optional tokenizer for token descriptions.</param>
        /// <returns>The string form.</returns>
        public string ToString(Tokenizer? tokenizer)
        {
            StringBuilder buffer = new();

            _ = buffer.Append("{");
            for (int i = 0; i < _elements.Count; i++)
            {
                Sequence seq = (Sequence)_elements[i]!;
                _ = buffer.Append("\n  ");
                _ = buffer.Append(seq.ToString(tokenizer));
            }
            _ = buffer.Append("\n}");
            return buffer.ToString();
        }

        private class Sequence
        {
            private bool _repeat;
            private readonly ArrayList _tokens;

            public Sequence()
            {
                _repeat = false;
                _tokens = [];
            }

            public Sequence(int token)
            {
                _repeat = false;
                _tokens = [token];
            }

            public Sequence(int length, Sequence seq)
            {
                _repeat = seq._repeat;
                _tokens = new ArrayList(length);
                if (seq.Length() < length)
                {
                    length = seq.Length();
                }
                for (int i = 0; i < length; i++)
                {
                    _ = _tokens.Add(seq._tokens[i]);
                }
            }

            public Sequence(bool repeat, Sequence seq)
            {
                _repeat = repeat;
                _tokens = seq._tokens;
            }

            public int Length()
            {
                return _tokens.Count;
            }

            public object? GetToken(int pos)
            {
                return pos >= 0 && pos < _tokens.Count ? _tokens[pos] : null;
            }

            public override bool Equals(object? obj)
            {
                return obj is Sequence sequence && Equals(sequence);
            }

            public bool Equals(Sequence seq)
            {
                if (_tokens.Count != seq._tokens.Count)
                {
                    return false;
                }
                for (int i = 0; i < _tokens.Count; i++)
                {
                    if (!_tokens[i]!.Equals(seq._tokens[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public override int GetHashCode()
            {
                return _tokens.Count.GetHashCode();
            }

            public bool StartsWith(Sequence seq)
            {
                if (Length() < seq.Length())
                {
                    return false;
                }
                for (int i = 0; i < seq._tokens.Count; i++)
                {
                    if (!_tokens[i]!.Equals(seq._tokens[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public bool IsRepetitive()
            {
                return _repeat;
            }

            public bool IsNext(Parser parser)
            {
                for (int i = 0; i < _tokens.Count; i++)
                {
                    var id = (int)_tokens[i]!;
                    var token = parser.PeekToken(i);
                    if (token == null || token.Id != id)
                    {
                        return false;
                    }
                }
                return true;
            }

            public bool IsNext(Parser parser, int length)
            {
                if (length > _tokens.Count)
                {
                    length = _tokens.Count;
                }
                for (int i = 0; i < length; i++)
                {
                    var id = (int)_tokens[i]!;
                    var token = parser.PeekToken(i);
                    if (token == null || token.Id != id)
                    {
                        return false;
                    }
                }
                return true;
            }

            public override string ToString()
            {
                return ToString(null);
            }

            public string ToString(Tokenizer? tokenizer)
            {
                StringBuilder buffer = new();

                if (tokenizer == null)
                {
                    _ = buffer.Append(_tokens.ToString());
                }
                else
                {
                    _ = buffer.Append("[");
                    for (int i = 0; i < _tokens.Count; i++)
                    {
                        var id = (int)_tokens[i]!;
                        var str = tokenizer.GetPatternDescription(id);
                        if (i > 0)
                        {
                            _ = buffer.Append(" ");
                        }
                        _ = buffer.Append(str);
                    }
                    _ = buffer.Append("]");
                }
                if (_repeat)
                {
                    _ = buffer.Append(" *");
                }
                return buffer.ToString();
            }

            public Sequence Concat(int length, Sequence seq)
            {
                Sequence res = new(length, this);

                if (seq._repeat)
                {
                    res._repeat = true;
                }
                length -= Length();
                if (length > seq.Length())
                {
                    res._tokens.AddRange(seq._tokens);
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        _ = res._tokens.Add(seq._tokens[i]);
                    }
                }
                return res;
            }

            public Sequence Subsequence(int start)
            {
                Sequence res = new(Length(), this);

                while (start > 0 && res._tokens.Count > 0)
                {
                    res._tokens.RemoveAt(0);
                    start--;
                }
                return res;
            }
        }
    }
}
