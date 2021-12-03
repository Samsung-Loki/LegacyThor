using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin a session
    /// </summary>
    public class SessionSetupPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x64); // Session control type
            stream.Write(0);    // Begin session
            stream.Write(0x4);  // Protocol v4
            //TODO: Add ability to change protocol version
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}