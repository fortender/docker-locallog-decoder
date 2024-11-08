﻿namespace LocalLogDecoder
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: <file> [..files]");
                return;
            }

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            await foreach (var entry in LocalLogParser.ReadEntries(args, cts.Token))
            {
                Console.WriteLine("{0:yyyy-MM-ddTHH:mm:ss.fffffff}: {1}", entry.Timestamp, entry.Line);
            }
        }
    }
}
