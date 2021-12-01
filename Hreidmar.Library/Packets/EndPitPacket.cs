using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// General PIT end
    /// </summary>
    public class EndPitPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[8];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x65); // PIT type
            stream.Write(0x03); // End flag
            return buf;
        }
    }
}