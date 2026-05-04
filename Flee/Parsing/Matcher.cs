namespace Flee.Parsing
{
    /// <summary>
    /// A regular-expression string matcher. Pairs a compiled <see cref="Element"/> tree with
    /// the input <see cref="ReaderBuffer"/> and tracks per-match state (current position,
    /// last match length, end-of-stream flag, case sensitivity). Not thread-safe.
    /// </summary>
    internal class Matcher
    {
        private readonly Element _element;
        private ReaderBuffer _buffer;
        private readonly bool _ignoreCase;
        private int _start;
        private int _length;
        private bool _endOfString;

        /// <summary>
        /// Initializes a new matcher.
        /// </summary>
        /// <param name="e">The compiled regex element.</param>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="ignoreCase">Whether matching is case-insensitive.</param>
        internal Matcher(Element e, ReaderBuffer buffer, bool ignoreCase)
        {
            _element = e;
            _buffer = buffer;
            _ignoreCase = ignoreCase;
            _start = 0;
            Reset();
        }

        /// <summary>
        /// Returns whether matching is case-insensitive.
        /// </summary>
        /// <returns><see langword="true"/> when case is ignored.</returns>
        public bool IsCaseInsensitive()
        {
            return _ignoreCase;
        }

        /// <summary>
        /// Resets the matcher's per-match state without changing the input buffer.
        /// </summary>
        public void Reset()
        {
            _length = -1;
            _endOfString = false;
        }

        /// <summary>
        /// Resets the matcher with a new string input.
        /// </summary>
        /// <param name="str">The new input string.</param>
        public void Reset(string str)
        {
            Reset(new ReaderBuffer(new StringReader(str)));
        }

        /// <summary>
        /// Resets the matcher with a new buffer.
        /// </summary>
        /// <param name="buffer">The new input buffer.</param>
        public void Reset(ReaderBuffer buffer)
        {
            _buffer = buffer;
            Reset();
        }

        /// <summary>
        /// Returns the start position of the most recent match.
        /// </summary>
        /// <returns>The match start position.</returns>
        public int Start()
        {
            return _start;
        }

        /// <summary>
        /// Returns the end position of the most recent match.
        /// </summary>
        /// <returns>The match end position.</returns>
        public int End()
        {
            return _length > 0 ? _start + _length : _start;
        }

        /// <summary>
        /// Returns the length of the most recent match.
        /// </summary>
        /// <returns>The match length, or <c>-1</c> if there was no match.</returns>
        public int Length()
        {
            return _length;
        }

        /// <summary>
        /// Returns whether the matcher reached the end of the input during the last match.
        /// </summary>
        /// <returns><see langword="true"/> when end of input was reached.</returns>
        public bool HasReadEndOfString()
        {
            return _endOfString;
        }

        /// <summary>
        /// Attempts to match starting at position zero.
        /// </summary>
        /// <returns><see langword="true"/> when a match was found.</returns>
        public bool MatchFromBeginning()
        {
            return MatchFrom(0);
        }

        /// <summary>
        /// Attempts to match starting at <paramref name="pos"/>.
        /// </summary>
        /// <param name="pos">The starting position.</param>
        /// <returns><see langword="true"/> when a match was found.</returns>
        public bool MatchFrom(int pos)
        {
            Reset();
            _start = pos;
            _length = _element.Match(this, _buffer, _start, 0);
            return _length >= 0;
        }

        /// <summary>
        /// Returns the matched substring or an empty string if there was no match.
        /// </summary>
        /// <returns>The matched substring.</returns>
        public override string ToString()
        {
            return _length <= 0 ? "" : _buffer.Substring(_buffer.Position, _length);
        }

        internal void SetReadEndOfString()
        {
            _endOfString = true;
        }
    }
}
