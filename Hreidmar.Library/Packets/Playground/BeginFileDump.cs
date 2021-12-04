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
            stream.Write(0x00); // Unknown
            stream.Write(0x00); // Phone/AP
            stream.Write(0x00); // Unknown
            stream.Write(2);    // MMC
            stream.Write(16);   // VBMETA
            stream.Write(0x00); // Not last
            return memory.ToArray();
        }

        public int GetSize() => 8;
    }
}