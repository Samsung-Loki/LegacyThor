using System.IO;

namespace Hreidmar.Enigma.Packets.Outbound
{
    /// <summary>
    /// Reboot your device
    /// </summary>
    public class RebootDevicePacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.EndSession);
            stream.Write((int)EndSessionCommands.Reboot);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}