using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    public class PrintSalesCodePacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Session);
            stream.Write((int)SessionCommands.PrintSalesCode);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}