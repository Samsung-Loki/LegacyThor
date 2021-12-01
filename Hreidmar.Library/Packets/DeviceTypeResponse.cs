using System;
using System.IO;
using Hreidmar.Library.PIT;

namespace Hreidmar.Library.Packets
{
    /// <summary>
    /// Session setup response
    /// </summary>
    public class DeviceTypeResponse : IInboundPacket
    {
        public PitEntry.DeviceTypeEnum DeviceType;
        
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            if (stream.ReadInt32() != 0x64)
                throw new Exception("[DeviceTypeResponse] Packet type is invalid");
            DeviceType = (PitEntry.DeviceTypeEnum) stream.ReadInt32();
        }

        public int GetSize() => 4;
    }
}