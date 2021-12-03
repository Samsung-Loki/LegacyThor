using System.IO;
using Hreidmar.Library.Packets.Outbound;

namespace Hreidmar.Library.Packets.Playground
{
    public class BeginFileDump : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x66); // File type
            stream.Write(0x01); // Dump flag
            return memory.ToArray();
        }

        public int GetSize() => 8;
    }
}