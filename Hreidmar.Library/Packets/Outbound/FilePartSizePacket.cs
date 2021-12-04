using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// File part size packet
    /// </summary>
    public class FilePartSizePacket : IOutboundPacket
    {
        public int FileSize = 0;
        
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Session);
            stream.Write((int)SessionCommands.FilePartSize);
            stream.Write(FileSize);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}