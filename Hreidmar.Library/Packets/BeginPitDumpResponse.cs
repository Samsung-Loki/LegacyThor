using System;
using System.IO;

namespace Hreidmar.Library.Packets
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
            if (stream.ReadInt32() != 0x65)
                throw new Exception("[DeviceTypeResponse] Packet type is invalid");
            Length = stream.ReadInt32();
        }

        public int GetSize() => 4;
    }
}