using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin PIT dump
    /// </summary>
    public class PitFlashPacket : IOutboundPacket
    {
        public int Length;
    
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Pit);
            stream.Write((int)SharedCommands.FilePart); // It's begin, actually.
            stream.Write(Length);
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}