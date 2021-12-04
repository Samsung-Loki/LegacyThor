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
            stream.Write((int)PacketType.Pit);
            stream.Write((int)SharedCommands.Flash);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}