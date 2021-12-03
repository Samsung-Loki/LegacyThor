using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Reports bytes count in total
    /// </summary>
    public class TotalBytesPacket : IOutboundPacket
    {
        public ulong Length;
        
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x64);   // Session type
            stream.Write(0x02);   // Total bytes flag
            stream.Write(Length); // Total bytes count
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}