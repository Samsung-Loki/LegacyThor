using System;
using System.IO;

namespace Hreidmar.Library.Packets.Inbound
{
    /// <summary>
    /// Session setup response
    /// </summary>
    public class EndSessionResponse : IInboundPacket
    {
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            if (stream.ReadInt32() != (int)PacketType.EndSession)
                throw new Exception("Packet type is invalid");
        }

        public int GetSize() => 8;
    }
}