using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin a session
    /// </summary>
    public class SessionSetupPacket : IOutboundPacket
    {
        public ProtocolVersion Version = ProtocolVersion.Version4;
        
        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write((int)PacketType.Session);
            stream.Write((int)SessionCommands.Begin);
            stream.Write((int)Version);
            //TODO: Add ability to change protocol version
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}