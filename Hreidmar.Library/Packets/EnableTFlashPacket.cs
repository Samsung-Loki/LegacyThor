using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// Enable T-Flash
    /// </summary>
    public class EnableTFlashPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[8];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x64); // Session type
            stream.Write(0x05); // Device type flag
            return buf;
        }
    }
}