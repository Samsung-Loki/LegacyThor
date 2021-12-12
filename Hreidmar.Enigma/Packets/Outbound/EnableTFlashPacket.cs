using System.IO;

namespace Hreidmar.Enigma.Packets.Outbound
{
    /// <summary>
    /// Enable T-Flash
    /// </summary>
    public class EnableTFlashPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Session); // Session type
            stream.Write((int)SessionCommands.TFlash); // T-Flash flag
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}