using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// Begin a session
    /// </summary>
    public class SessionSetupPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[8];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x64); // Session control type
            stream.Write(0x00); // Begin session
            stream.Write(0x04); // Protocol v4
            // TODO: Add ability to change protocol version
            return buf;
        }
    }
}