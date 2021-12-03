using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin file flash
    /// </summary>
    public class BeginFileFlashPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x66); // File type
            stream.Write(0x00); // Flash flag
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}