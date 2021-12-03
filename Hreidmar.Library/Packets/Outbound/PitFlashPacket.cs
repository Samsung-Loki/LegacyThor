using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin PIT dump
    /// </summary>
    public class PitFlashPacket : IOutboundPacket
    {
        public int Length;
    
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x65);   // PIT type
            stream.Write(0x02);   // Begin flag
            stream.Write(Length); // Byte length
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}