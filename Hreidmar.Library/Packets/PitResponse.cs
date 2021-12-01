using System;
using System.IO;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// General PIT response
    /// </summary>
    public class PitResponse : IInboundPacket
    {
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            if (stream.ReadInt32() != 0x65)
                throw new Exception("[DeviceTypeResponse] Packet type is invalid");
        }

        public int GetSize() => 4;
    }
}