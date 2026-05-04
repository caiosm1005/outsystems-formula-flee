namespace Flee.Parsing
{
    /// <summary>
    /// A look-ahead character stream reader. Provides the functionality of a buffered
    /// line-number reader with the additional ability to peek an unlimited number of
    /// characters ahead. As look-ahead deepens, the buffer grows to hold every character from
    /// the current position onward; very deep look-ahead is therefore memory-bounded.
    /// </summary>
    /// <param name="input">The underlying text reader to wrap.</param>
    internal class LookAheadReader(TextReader input) : TextReader()
    {
        private const int StreamBlockSize = 4096;
        private const int BufferBlockSize = 1024;
        private char[] _buffer = new char[StreamBlockSize];
        private int _pos;
        private int _length;
        private TextReader? _input = input;

        /// <summary>
        /// Gets the one-based line number of the next character to be read.
        /// </summary>
        public int LineNumber { get; private set; } = 1;

        /// <summary>
        /// Gets the one-based column number of the next character to be read.
        /// </summary>
        public int ColumnNumber { get; private set; } = 1;

        /// <summary>
        /// Reads the next character and advances the position.
        /// </summary>
        /// <returns>The character read, or <c>-1</c> at end of stream.</returns>
        public override int Read()
        {
            ReadAhead(1);
            if (_pos >= _length)
            {
                return -1;
            }
            else
            {
                UpdateLineColumnNumbers(1);
                return Convert.ToInt32(_buffer[Math.Max(Interlocked.Increment(ref _pos), _pos - 1)]);
            }
        }

        /// <summary>
        /// Reads up to <paramref name="len"/> characters into <paramref name="cbuf"/>.
        /// </summary>
        /// <param name="cbuf">The destination buffer.</param>
        /// <param name="off">The offset in <paramref name="cbuf"/> at which to begin writing.</param>
        /// <param name="len">The maximum number of characters to read.</param>
        /// <returns>The number of characters read, or <c>-1</c> at end of stream.</returns>
        public override int Read(char[] cbuf, int off, int len)
        {
            ReadAhead(len);
            if (_pos >= _length)
            {
                return -1;
            }
            else
            {
                var count = _length - _pos;
                if (count > len)
                {
                    count = len;
                }
                UpdateLineColumnNumbers(count);
                Array.Copy(_buffer, _pos, cbuf, off, count);
                _pos += count;
                return count;
            }
        }

        /// <summary>
        /// Reads up to <paramref name="len"/> characters and returns them as a string.
        /// </summary>
        /// <param name="len">The maximum number of characters to read.</param>
        /// <returns>The characters read, or <see langword="null"/> at end of stream.</returns>
        public string? ReadString(int len)
        {
            ReadAhead(len);
            if (_pos >= _length)
            {
                return null;
            }
            else
            {
                var count = _length - _pos;
                if (count > len)
                {
                    count = len;
                }
                UpdateLineColumnNumbers(count);
                var result = new string(_buffer, _pos, count);
                _pos += count;
                return result;
            }
        }

        /// <summary>
        /// Returns the next character without advancing the position.
        /// </summary>
        /// <returns>The character peeked, or <c>-1</c> at end of stream.</returns>
        public override int Peek()
        {
            return Peek(0);
        }

        /// <summary>
        /// Returns the character at offset <paramref name="off"/> from the current position
        /// without advancing.
        /// </summary>
        /// <param name="off">The zero-based offset to peek.</param>
        /// <returns>The character peeked, or <c>-1</c> at end of stream.</returns>
        public int Peek(int off)
        {
            ReadAhead(off + 1);
            return _pos + off >= _length ? -1 : Convert.ToInt32(_buffer[_pos + off]);
        }

        /// <summary>
        /// Returns up to <paramref name="len"/> characters at offset <paramref name="off"/>
        /// from the current position without advancing.
        /// </summary>
        /// <param name="off">The zero-based offset to peek.</param>
        /// <param name="len">The maximum number of characters to peek.</param>
        /// <returns>The characters peeked, or <see langword="null"/> at end of stream.</returns>
        public string? PeekString(int off, int len)
        {
            ReadAhead(off + len + 1);
            if (_pos + off >= _length)
            {
                return null;
            }
            else
            {
                var count = _length - (_pos + off);
                if (count > len)
                {
                    count = len;
                }
                return new string(_buffer, _pos + off, count);
            }
        }

        /// <summary>
        /// Closes the reader and releases the underlying buffer.
        /// </summary>
        public override void Close()
        {
            _buffer = null!;
            _pos = 0;
            _length = 0;
            _input?.Close();
            _input = null;
        }

        private void ReadAhead(int offset)
        {
            // Check for end of stream or already read characters
            if (_input == null || _pos + offset < _length)
            {
                return;
            }

            // Remove old characters from buffer
            if (_pos > BufferBlockSize)
            {
                Array.Copy(_buffer, _pos, _buffer, 0, _length - _pos);
                _length -= _pos;
                _pos = 0;
            }

            // Calculate number of characters to read
            int size = _pos + offset - _length + 1;
            if (size % StreamBlockSize != 0)
            {
                size = size / StreamBlockSize * StreamBlockSize;
                size += StreamBlockSize;
            }
            EnsureBufferCapacity(_length + size);

            // Read characters
            int readSize;
            try
            {
                readSize = _input.Read(_buffer, _length, size);
            }
            catch (IOException)
            {
                _input = null;
                throw;
            }

            // Append characters to buffer
            if (readSize > 0)
            {
                _length += readSize;
            }
            if (readSize < size)
            {
                try
                {
                    _input.Close();
                }
                finally
                {
                    _input = null;
                }
            }
        }

        private void EnsureBufferCapacity(int size)
        {
            char[] newbuf;

            if (_buffer.Length >= size)
            {
                return;
            }
            if (size % BufferBlockSize != 0)
            {
                size = size / BufferBlockSize * BufferBlockSize;
                size += BufferBlockSize;
            }
            newbuf = new char[size];
            Array.Copy(_buffer, 0, newbuf, 0, _length);
            _buffer = newbuf;
        }

        private void UpdateLineColumnNumbers(int offset)
        {
            for (int i = 0; i <= offset - 1; i++)
            {
                if (_buffer.Contains(_buffer[_pos + i]))
                {
                    LineNumber += 1;
                    ColumnNumber = 1;
                }
                else
                {
                    ColumnNumber += 1;
                }
            }
        }
    }
}
