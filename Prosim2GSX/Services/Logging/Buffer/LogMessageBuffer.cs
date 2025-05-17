using Prosim2GSX.Services.Logging.Models;
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services.Logging.Buffer
{
    /// <summary>
    /// A circular buffer for log messages
    /// </summary>
    public class LogMessageBuffer
    {
        private readonly LogMessage[] _buffer;
        private readonly int _bufferSize;
        private long _nextIndex = 0;
        private int _count = 0;
        private readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the LogMessageBuffer class
        /// </summary>
        /// <param name="bufferSize">The size of the buffer</param>
        public LogMessageBuffer(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be greater than 0");

            _bufferSize = bufferSize;
            _buffer = new LogMessage[bufferSize];
        }

        /// <summary>
        /// Adds a log message to the buffer
        /// </summary>
        /// <param name="message">The log message to add</param>
        /// <returns>The id assigned to the message</returns>
        public long Add(LogMessage message)
        {
            lock (_lock)
            {
                long id = _nextIndex++;
                int index = (int)(id % _bufferSize);
                _buffer[index] = message;
                _count = Math.Min(_count + 1, _bufferSize);
                return id;
            }
        }

        /// <summary>
        /// Gets all messages in the buffer
        /// </summary>
        /// <returns>A list of log messages</returns>
        public List<LogMessage> GetMessages()
        {
            lock (_lock)
            {
                var result = new List<LogMessage>(_count);

                if (_count < _bufferSize)
                {
                    // Buffer hasn't wrapped yet
                    for (int i = 0; i < _count; i++)
                    {
                        if (_buffer[i] != null)
                            result.Add(_buffer[i]);
                    }
                }
                else
                {
                    // Buffer has wrapped, start from oldest item
                    long oldestId = _nextIndex - _count;
                    int oldestIndex = (int)(oldestId % _bufferSize);

                    for (int i = 0; i < _bufferSize; i++)
                    {
                        int index = (oldestIndex + i) % _bufferSize;
                        if (_buffer[index] != null)
                            result.Add(_buffer[index]);
                    }
                }

                result.Sort((a, b) => a.Id.CompareTo(b.Id));
                return result;
            }
        }

        /// <summary>
        /// Gets filtered messages from the buffer
        /// </summary>
        /// <param name="predicate">The filter predicate</param>
        /// <returns>A list of matching log messages</returns>
        public List<LogMessage> GetFilteredMessages(Func<LogMessage, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var allMessages = GetMessages();
            return allMessages.FindAll(msg => predicate(msg));
        }

        /// <summary>
        /// Clears all messages in the buffer
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _count = 0;
            }
        }
    }
}
