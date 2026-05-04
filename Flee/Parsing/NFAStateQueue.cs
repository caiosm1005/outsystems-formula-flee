namespace Flee.Parsing
{
    /// <summary>
    /// A queue of NFA states. Used during NFA processing to keep track of the current and
    /// subsequent states; the current state is read from the front and new states are added at
    /// the back, with a marker index separating the two halves.
    /// </summary>
    /// <remarks>
    /// The queue is optimized for quick removal at the front and addition at the back. It uses
    /// a fixed-size array for storage and only moves data when absolutely necessary; the array
    /// is enlarged automatically if too many states are processed at once.
    /// </remarks>
    internal class NFAStateQueue
    {

        private NFAState[] _queue = new NFAState[2048];

        private int _first = 0;

        private int _last = 0;

        private int _mark = 0;

        /// <summary>
        /// Returns whether the queue currently has no entries between the head and the back.
        /// </summary>
        public bool Empty => _last <= _first;

        /// <summary>
        /// Returns whether the head of the queue has reached the marker (i.e. all entries
        /// from the previous frame have been consumed).
        /// </summary>
        public bool Marked => _first == _mark;

        /// <summary>
        /// Resets the queue to an empty state without releasing its underlying buffer.
        /// </summary>
        public void Clear()
        {
            _first = 0;
            _last = 0;
            _mark = 0;
        }

        /// <summary>
        /// Marks the current end of the queue. Subsequent additions belong to the next frame.
        /// </summary>
        public void MarkEnd()
        {
            _mark = _last;
        }

        /// <summary>
        /// Removes and returns the state at the front of the queue.
        /// </summary>
        /// <returns>The removed state, or <see langword="null"/> when the queue is empty.</returns>
        public NFAState? RemoveFirst()
        {
            if (_first < _last)
            {
                _first++;
                return _queue[_first - 1];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Appends <paramref name="state"/> to the back of the queue, growing or compacting
        /// the underlying buffer as needed.
        /// </summary>
        /// <param name="state">The state to append.</param>
        public void AddLast(NFAState state)
        {
            if (_last >= _queue.Length)
            {
                if (_first <= 0)
                {
                    Array.Resize(ref _queue, _queue.Length * 2);
                }
                else
                {
                    Array.Copy(_queue, _first, _queue, 0, _last - _first);
                    _last -= _first;
                    _mark -= _first;
                    _first = 0;
                }
            }
            _queue[_last++] = state;
        }
    }
}
