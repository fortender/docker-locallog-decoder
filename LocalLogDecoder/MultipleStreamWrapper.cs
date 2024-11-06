namespace LocalLogDecoder
{
    /// <summary>
    /// Creates a read-only stream over an enumerable of streams. This is used to "concatenate" file streams.
    /// </summary>
    public class MultipleStreamWrapper : Stream
    {

        private readonly Queue<Stream> _streams;
        private Stream _current;

        public MultipleStreamWrapper(IEnumerable<Stream> streams)
        {
            _streams = new Queue<Stream>(streams);
            _current = _streams.Dequeue();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readBytes = _current.Read(buffer, offset, count);
            if (readBytes == 0)
            {
                _current.Dispose();
                if (!_streams.TryDequeue(out var stream))
                    return 0;
                _current = stream;
                return Read(buffer, offset, count);
            }
            return readBytes;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int _disposed;

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            if (disposing)
            {
                _current.Dispose();
                while (_streams.TryDequeue(out Stream? s))
                    s.Dispose();
            }
        }

        public override async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            await Task.WhenAll(_streams.Select(s => s.DisposeAsync().AsTask()).Append(_current.DisposeAsync().AsTask()));
        }

    }

}
