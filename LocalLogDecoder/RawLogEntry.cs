namespace LocalLogDecoder
{
    public record RawLogEntry(string Source, DateTimeOffset Timestamp, string Line);
}
