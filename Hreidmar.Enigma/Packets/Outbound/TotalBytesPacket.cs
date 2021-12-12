using System.IO;

namespace Hreidmar.Enigma.Packets.Outbound
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
            stream.Write((int)PacketType.Session);
            stream.Write((int)SessionCommands.TotalBytes);
            stream.Write(Length);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}