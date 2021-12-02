using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// Begin PIT dump
    /// </summary>
    public class BeginPitDumpPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x65); // PIT type
            stream.Write(0x01); // Dump flag
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}