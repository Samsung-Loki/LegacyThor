using System.IO;

namespace Hreidmar.Enigma.Packets.Outbound
{
    /// <summary>
    /// General PIT end
    /// </summary>
    public class EndPitPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Pit);
            stream.Write((int)SharedCommands.End);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}