using System.IO;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// Begin a session
    /// </summary>
    public class SessionSetupPacket : IOutboundPacket
    {
        public DeviceSession.Protocol protocol;

        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x64); // Session control type
            stream.Write(0);    // Begin session

            switch(protocol)
            {
                case DeviceSession.Protocol.ODIN_PROTOCOL_V3:
                    stream.Write(0x3); // Protocol v3
                    break;
                case DeviceSession.Protocol.ODIN_PROTOCOL_V4:
                    stream.Write(0x4); // Protocol v4
                    break;
                default:
                    throw new System.Exception(string.Format("Invalid Protocol Version: {0}.", protocol.ToString()));
            }

            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}