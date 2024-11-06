﻿
namespace LocalLogDecoder.Tests
{
    public class LocalLogParserTests
    {

        static readonly byte[] SampleData =
        [
            // First entry
            0x00, 0x00, 0x00, 0x1F, 0x0A, 0x06, 0x73, 0x74, 0x64, 0x6F, 0x75, 0x74, 0x10, 0x80, 0xB0, 0xB0,
            0xC9, 0xC4, 0xC5, 0xD5, 0x82, 0x18, 0x1A, 0x0B, 0x4C, 0x6F, 0x67, 0x20, 0x65, 0x6E, 0x74, 0x72,
            0x79, 0x20, 0x31, 0x00, 0x00, 0x00, 0x1F,

            // Second entry
            0x00, 0x00, 0x00, 0x1F, 0x0A, 0x06, 0x73, 0x74, 0x64, 0x6F, 0x75, 0x74, 0x10, 0x80, 0xC4, 0x9B,
            0xA6, 0xC8, 0xC5, 0xD5, 0x82, 0x18, 0x1A, 0x0B, 0x4C, 0x6F, 0x67, 0x20, 0x65, 0x6E, 0x74, 0x72,
            0x79, 0x20, 0x32, 0x00, 0x00, 0x00, 0x1F
        ];

        [Fact]
        public void ReadEntries_ParsesDataCorrectly()
        {
            var entries = LocalLogParser.ReadEntries(new MemoryStream(SampleData, false), CancellationToken.None).ToBlockingEnumerable().ToArray();
            
            Assert.Equivalent(new[] {
                new RawLogEntry("stdout", new DateTimeOffset(2024, 11, 6, 9, 13, 0, TimeSpan.Zero), "Log entry 1"),
                new RawLogEntry("stdout", new DateTimeOffset(2024, 11, 6, 9, 13, 1, TimeSpan.Zero), "Log entry 2")
            }, entries);
        }

    }

}