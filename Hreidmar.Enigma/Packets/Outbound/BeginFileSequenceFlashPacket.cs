using System.IO;

namespace Hreidmar.Enigma.Packets.Outbound
{
    /// <summary>
    /// Begin file sequence flash
    /// </summary>
    public class BeginFileSequenceFlashPacket : IOutboundPacket
    {
        public int Length;
        
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.File);
            stream.Write((int)SharedCommands.FilePart);
            stream.Write(Length); // Size
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}