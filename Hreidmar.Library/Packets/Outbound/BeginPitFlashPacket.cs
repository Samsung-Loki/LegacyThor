using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin PIT flash
    /// </summary>
    public class BeginPitFlashPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x65); // PIT type
            stream.Write(0x00); // Flash flag
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}