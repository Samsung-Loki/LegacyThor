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
            stream.Write((int)PacketType.File);
            stream.Write((int)SharedCommands.Flash);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}