using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin PIT dump
    /// </summary>
    public class DumpPitPacket : IOutboundPacket
    {
        public int Block;
        
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Pit);
            stream.Write((int)SharedCommands.FilePart);
            stream.Write(Block); // Current block
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}