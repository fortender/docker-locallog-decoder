using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace LocalLogDecoder
{
    public static class LocalLogParser
    {

        public static IAsyncEnumerable<RawLogEntry> ReadEntries(string fileName, CancellationToken cancellationToken)
            => ReadEntries([fileName], cancellationToken);

        public static async IAsyncEnumerable<RawLogEntry> ReadEntries(string[] fileNames, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var fs = new MultipleStreamWrapper(fileNames.Select(File.OpenRead));
            var reader = PipeReader.Create(fs, new StreamPipeReaderOptions(bufferSize: 8192));

            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadEntry(ref buffer, out var entry))
                {
                    yield return entry;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }

            static bool TryReadEntry(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out RawLogEntry? entry)
            {
                var reader = new SequenceReader<byte>(buffer);

                // [len(entry):4] [entry:len] [len(entry):4]
                if (!reader.TryReadBigEndian(out int entryLength) ||
                    !reader.TryReadExact(entryLength + 4, out ReadOnlySequence<byte> entryData))
                {
                    entry = null;
                    return false;
                }

                buffer = buffer.Slice(entryLength + 8);
                entry = ParseEntry(entryData.Slice(0, entryLength));
                return true;
            }

            static RawLogEntry ParseEntry(ReadOnlySequence<byte> buffer)
            {
                var reader = new SequenceReader<byte>(buffer);

                // Fields of interest
                string source = string.Empty;
                DateTimeOffset timestamp = default;
                string line = string.Empty;

                while (reader.TryRead(out byte tp))
                {
                    int fieldNumber = tp >> 3;
                    WireType wireType = (WireType)(tp & 0b111);

                    /* Protobuf definition
                    message LogEntry {
	                    string source = 1;
	                    int64 time_nano = 2;
	                    bytes line = 3;
                        ...
                    }
                    */
                    switch (wireType)
                    {
                        case WireType.LEN when fieldNumber == 1:
                            source = ReadString(ref reader);
                            break;
                        case WireType.VARINT when fieldNumber == 2:
                            ulong timeNano = ReadVarInt(ref reader);
                            timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(timeNano / 1_000_000));
                            break;
                        case WireType.LEN when fieldNumber == 3:
                            line = ReadString(ref reader);
                            break;
                    }
                }

                return new RawLogEntry(source, timestamp, line);
            }

            // Reads a protobuf VARINT
            static ulong ReadVarInt(ref SequenceReader<byte> reader)
            {
                ulong value = 0;
                int count = 0;
                while (true)
                {
                    if (!reader.TryRead(out byte part))
                        throw new InvalidOperationException();

                    value |= ((ulong)(part & ~0x80)) << (7 * count++);

                    // While MSB = 1 we need to keep reading
                    if ((part & 0x80) == 0)
                        break;
                }
                return value;
            }

            // Reads a protobuf string (wire type: LEN)
            static string ReadString(ref SequenceReader<byte> reader)
            {
                int length = (int)ReadVarInt(ref reader);
                if (!reader.TryReadExact(length, out var seq))
                    throw new InvalidOperationException();
                return Encoding.UTF8.GetString(seq);
            }

        }

        enum WireType
        {
            VARINT,
            I64,
            LEN,
            SGROUP,
            EGROUP,
            I32
        }

    }
}
