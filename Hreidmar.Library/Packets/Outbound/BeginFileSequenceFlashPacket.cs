using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin file sequence flash
    /// </summary>
    public class BeginFileSequenceFlashPacket : IOutboundPacket
    {
        public int Length;
        
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x66);   // File type
            stream.Write(0x02);   // Flash flag
            stream.Write(Length); // Size
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}