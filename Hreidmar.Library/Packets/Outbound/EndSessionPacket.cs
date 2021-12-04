using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Reboot your device
    /// </summary>
    public class EndSessionPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x67); // End session control type
            stream.Write(0);    // End session flag
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}