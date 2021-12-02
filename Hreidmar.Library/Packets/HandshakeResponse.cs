using System;
using System.IO;
using System.Text;
using Spectre.Console;

namespace Hreidmar.Library.Packets
{
    public class HandshakeResponse : IInboundPacket
    {
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            var str = Encoding.ASCII.GetString(stream.ReadBytes(4));
            if (str != "LOKE")
                throw new Exception($"[HandshakeResponse] Invalid response: {str}");
            AnsiConsole.MarkupLine($"[bold]<HandshakeResponse>[/] Received {str}");
        }

        public int GetSize() => 4;
    }
}