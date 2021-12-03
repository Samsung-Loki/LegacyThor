using System;
using System.IO;
using Hreidmar.Library.Packets.Inbound;
using Spectre.Console;

namespace Hreidmar.Library.Packets.Playground
{
    public class PlaygroundResponse : IInboundPacket
    {
        public void Unpack(byte[] buf)
        {
            var rng = new Random();
            var filename = $"dump-{rng.Next()}{rng.Next()}{rng.Next()}{rng.Next()}.bin";
            AnsiConsole.WriteLine($"RESPONSE DUMPED: {filename}");
            File.WriteAllBytes(filename, buf);
        }

        public int GetSize() => 1024;
    }
}