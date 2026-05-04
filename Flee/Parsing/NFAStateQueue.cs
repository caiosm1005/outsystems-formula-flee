namespace Flee.Parsing
{
    /**
     * An NFA state queue. This queue is used during processing to
     * keep track of the current and subsequent NFA states. The
     * current state is read from the beginning of the queue, and new
     * states are added at the end. A marker index is used to
     * separate the current from the subsequent states.<p>
     *
     * The queue implementation is optimized for quick removal at the
     * beginning and addition at the end. It will attempt to use a
     * fixed-size array to store the whole queue, and moves the data
     * in this array only when absolutely needed. The array is also
     * enlarged automatically if too many states are being processed
     * at a single time.
     */
    internal class NFAStateQueue
    {

        private NFAState[] _queue = new NFAState[2048];

        private int _first = 0;

        private int _last = 0;

        private int _mark = 0;

        public bool Empty => _last <= _first;

        public bool Marked => _first == _mark;

        public void Clear()
        {
            _first = 0;
            _last = 0;
            _mark = 0;
        }

        public void MarkEnd()
        {
            _mark = _last;
        }

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
