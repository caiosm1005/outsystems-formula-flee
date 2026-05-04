namespace Flee.Parsing
{
    /// <summary>
    /// A character buffer that automatically reads from an input source stream when needed.
    /// Tracks the current position in the buffer along with its line and column number in the
    /// original source. Supports unlimited look-ahead, internally buffering more characters as
    /// needed; as the position advances, content prior to it may be discarded to free space,
    /// keeping a few characters around for boundary checks.
    /// </summary>
    /// <param name="input">The underlying input source.</param>
    internal class ReaderBuffer(TextReader input)
    {
        /// <summary>
        /// The block size used for buffer growth and the underlying read loop.
        /// </summary>
        public const int BlockSize = 1024;

        private char[] _buffer = new char[BlockSize * 4];
        private TextReader? _input = input;

        /// <summary>
        /// Releases the underlying buffer and closes the wrapped input source.
        /// </summary>
        public void Dispose()
        {
            _buffer = null!;
            Position = 0;
            Length = 0;
            if (_input != null)
            {
                try
                {
                    _input.Close();
                }
                catch (Exception)
                {
                    // Do nothing
                }
                _input = null;
            }
        }

        /// <summary>
        /// Gets the current read position within the buffer.
        /// </summary>
        public int Position { get; private set; } = 0;

        /// <summary>
        /// Gets the one-based line number of the next character to be read.
        /// </summary>
        public int LineNumber { get; private set; } = 1;

        /// <summary>
        /// Gets the one-based column number of the next character to be read.
        /// </summary>
        public int ColumnNumber { get; private set; } = 1;

        /// <summary>
        /// Gets the number of characters currently buffered.
        /// </summary>
        public int Length { get; private set; } = 0;

        /// <summary>
        /// Returns the substring of length <paramref name="length"/> starting at
        /// <paramref name="index"/> in the buffer.
        /// </summary>
        /// <param name="index">The starting offset.</param>
        /// <param name="length">The substring length.</param>
        /// <returns>The substring.</returns>
        public string Substring(int index, int length)
        {
            return new string(_buffer, index, length);
        }

        /// <summary>
        /// Returns the entire buffered content as a string.
        /// </summary>
        /// <returns>The buffered content.</returns>
        public override string ToString()
        {
            return new string(_buffer, 0, Length);
        }

        /// <summary>
        /// Returns the character at <paramref name="offset"/> from the current position
        /// without advancing.
        /// </summary>
        /// <param name="offset">The zero-based offset to peek.</param>
        /// <returns>The character peeked, or <c>-1</c> at end of stream.</returns>
        public int Peek(int offset)
        {
            int index = Position + offset;

            // Avoid most calls to EnsureBuffered(), since we are in a
            // performance hotspot here. This check is not exhaustive,
            // but only present here to speed things up.
            if (index >= Length)
            {
                EnsureBuffered(offset + 1);
                index = Position + offset;
            }
            return (index >= Length) ? -1 : _buffer[index];
        }

        /// <summary>
        /// Reads up to <paramref name="offset"/> characters from the buffer, advancing the
        /// position and updating line and column counters.
        /// </summary>
        /// <param name="offset">The maximum number of characters to read.</param>
        /// <returns>The characters read, or <see langword="null"/> at end of stream.</returns>
        public string? Read(int offset)
        {
            EnsureBuffered(offset + 1);
            if (Position >= Length)
            {
                return null;
            }
            else
            {
                var count = Length - Position;
                if (count > offset)
                {
                    count = offset;
                }
                UpdateLineColumnNumbers(count);
                var result = new string(_buffer, Position, count);
                Position += count;
                if (_input == null && Position >= Length)
                {
                    Dispose();
                }
                return result;
            }
        }

        private void UpdateLineColumnNumbers(int offset)
        {
            for (int i = 0; i < offset; i++)
            {
                if (_buffer[Position + i] == '\n')
                {
                    LineNumber++;
                    ColumnNumber = 1;
                }
                else
                {
                    ColumnNumber++;
                }
            }
        }

        private void EnsureBuffered(int offset)
        {
            // Check for end of stream or already read characters
            if (_input == null || Position + offset < Length)
            {
                return;
            }

            // Remove (almost all) old characters from buffer
            if (Position > BlockSize)
            {
                Length -= Position - 16;
                Array.Copy(_buffer, Position - 16, _buffer, 0, Length);
                Position = 16;
            }

            // Calculate number of characters to read
            var size = Position + offset - Length + 1;
            if (size % BlockSize != 0)
            {
                size = (1 + (size / BlockSize)) * BlockSize;
            }
            EnsureCapacity(Length + size);

            // Read characters
            try
            {
                while (_input != null && size > 0)
                {
                    var readSize = _input.Read(_buffer, Length, size);
                    if (readSize > 0)
                    {
                        Length += readSize;
                        size -= readSize;
                    }
                    else
                    {
                        _input.Close();
                        _input = null;
                    }
                }
            }
            catch (IOException)
            {
                _input = null;
                throw;
            }
        }

        private void EnsureCapacity(int size)
        {
            if (_buffer.Length >= size)
            {
                return;
            }
            if (size % BlockSize != 0)
            {
                size = (1 + (size / BlockSize)) * BlockSize;
            }
            Array.Resize(ref _buffer, size);
        }
    }
}
