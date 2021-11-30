using System;
using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// Session setup response
    /// </summary>
    public class SessionSetupResponse : IInboundPacket
    {
        public int Flags;
        
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            if (stream.ReadInt32() != 0x64)
                throw new Exception("[SessionSetupResponse] Packet type is invalid");
            Flags = stream.ReadInt32();
        }

        public int GetSize() => 8;
    }
}