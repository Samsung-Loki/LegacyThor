using System.IO;

namespace Hreidmar.Library.Packets
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
            stream.Write(0x64);     // Session control type
            stream.Write(5);        // File part size flag
            stream.Write(FileSize); // File part size value
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}