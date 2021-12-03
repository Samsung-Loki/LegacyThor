using System.IO;
using System.Text;

namespace Hreidmar.Library.Packets.Outbound
{
    public class HandshakePacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[4];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(Encoding.ASCII.GetBytes("ODIN"));
            return memory.ToArray();
        }
        
        public int GetSize() => 4;
    }
}