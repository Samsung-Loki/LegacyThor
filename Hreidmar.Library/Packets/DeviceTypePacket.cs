using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// Get device type
    /// </summary>
    public class DeviceTypePacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[8];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x64); // Session type
            stream.Write(0x01); // Device type flag
            return buf;
        }
    }
}