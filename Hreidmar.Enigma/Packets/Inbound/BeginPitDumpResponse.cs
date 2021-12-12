using System;
using System.IO;

namespace Hreidmar.Enigma.Packets.Inbound
{
    /// <summary>
    /// Begin PIT dump response
    /// </summary>
    public class BeginPitDumpResponse : IInboundPacket
    {
        public int Length;
        
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            if (stream.ReadInt32() != (int)PacketType.Pit)
                throw new Exception("Packet type is invalid");
            Length = stream.ReadInt32();
        }

        public int GetSize() => 8;
    }
}