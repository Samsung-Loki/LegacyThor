using System;
using System.IO;

namespace Hreidmar.Library.Packets.Inbound
{
    /// <summary>
    /// File part response
    /// </summary>
    public class FilePartResponse : IInboundPacket
    {
        public int Index;
        
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            if (stream.ReadInt32() != 0x00)
                throw new Exception("[FilePartResponse] Packet type is invalid");
            Index = stream.ReadInt32();
        }

        public int GetSize() => 8;
    }
}