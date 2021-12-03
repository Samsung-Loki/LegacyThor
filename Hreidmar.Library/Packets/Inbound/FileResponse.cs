using System;
using System.IO;

namespace Hreidmar.Library.Packets.Inbound
{
    /// <summary>
    /// General file response
    /// </summary>
    public class FileResponse : IInboundPacket
    {
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            if (stream.ReadInt32() != 0x66)
                throw new Exception("Packet type is invalid");
        }

        public int GetSize() => 8;
    }
}