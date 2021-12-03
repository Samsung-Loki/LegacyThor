using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin PIT dump
    /// </summary>
    public class DumpPitPacket : IOutboundPacket
    {
        public int Block;
        
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x65);  // PIT type
            stream.Write(0x02);  // File part flag
            stream.Write(Block); // Current block
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}