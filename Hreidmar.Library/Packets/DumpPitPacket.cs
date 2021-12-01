using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// Begin PIT dump
    /// </summary>
    public class DumpPitPacket : IOutboundPacket
    {
        public int Block;
        
        public byte[] Pack()
        {
            var buf = new byte[8];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x65);  // PIT type
            stream.Write(0x02);  // File part flag
            stream.Write(Block); // Current block
            return buf;
        }
    }
}