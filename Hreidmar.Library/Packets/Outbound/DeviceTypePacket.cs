using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Get device type
    /// </summary>
    public class DeviceTypePacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Session);
            stream.Write((int)SessionCommands.DeviceType);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}