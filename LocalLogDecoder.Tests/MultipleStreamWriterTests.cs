
namespace LocalLogDecoder.Tests
{
    public class MultipleStreamWriterTests
    {
        [Fact]
        public void Read_SeemlesslySwapsStreams()
        {
            byte[] data = Enumerable.Range(0, 100).Select(value => (byte)value).ToArray();
            var chunkedStreams = data.Chunk(10).Select(buf => new MemoryStream(buf));
            var combinedStream = new MultipleStreamWrapper(chunkedStreams);
            Span<byte> buffer = stackalloc byte[100];
            Span<byte> bufferSlice = buffer;
            int bytesRead;
            while ((bytesRead = combinedStream.Read(bufferSlice)) != 0)
            {
                bufferSlice = bufferSlice[bytesRead..];
            }
            Assert.True(buffer.SequenceEqual(data));
        }

        [Fact]
        public void Read_StreamIsDisposedOnExhaustion()
        {
            var s1 = new StreamWrapper(new MemoryStream([1, 2, 3, 4]));
            var s2 = new MemoryStream([5, 6, 7, 8]);
            using var combinedStream = new MultipleStreamWrapper(Enumerable.Empty<Stream>().Append(s1).Append(s2));
            Span<byte> buffer = stackalloc byte[8];

            // First read will exhaust first stream, but the dispose will first be called on the second read
            Assert.Equal(4, combinedStream.Read(buffer));
            Assert.Equal(4, combinedStream.Read(buffer));
            Assert.True(s1.IsDisposed);
        }

        [Fact]
        public void Dispose_DisposesAllInnerStreams()
        {
            StreamWrapper[] streams = [new StreamWrapper(Stream.Null), new StreamWrapper(Stream.Null), new StreamWrapper(Stream.Null)];
            new MultipleStreamWrapper(streams).Dispose();
            Assert.All(streams, stream => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task DisposeAsync_DisposesAllInnerStreams()
        {
            StreamWrapper[] streams = [new StreamWrapper(Stream.Null), new StreamWrapper(Stream.Null), new StreamWrapper(Stream.Null)];
            await new MultipleStreamWrapper(streams).DisposeAsync();
            Assert.All(streams, stream => Assert.True(stream.IsDisposed));
        }

        class StreamWrapper(Stream stream) : Stream
        {

            private readonly Stream _stream = stream;

            public override bool CanRead => _stream.CanRead;

            public override bool CanSeek => _stream.CanSeek;

            public override bool CanWrite => _stream.CanWrite;

            public override long Length => _stream.Length;

            public override long Position { get => _stream.Position; set => _stream.Position = value; }

            public override void Flush()
            {
                _stream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _stream.Write(buffer, offset, count);
            }

            private int _disposed;

            public bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) == 1;

            protected override void Dispose(bool disposing)
            {
                Interlocked.Exchange(ref _disposed, 1);
                base.Dispose(disposing);
            }

            public override ValueTask DisposeAsync()
            {
                Interlocked.Exchange(ref _disposed, 1);
                return base.DisposeAsync();
            }

        }

    }

}