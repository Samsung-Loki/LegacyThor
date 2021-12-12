using System;
using System.IO;
using Hreidmar.Enigma.PIT;

namespace Hreidmar.Enigma.Packets.Inbound
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
            if (stream.ReadInt32() != (int)PacketType.Session)
                throw new Exception("Packet type is invalid");
            DeviceType = (PitEntry.DeviceTypeEnum) stream.ReadInt32();
        }

        public int GetSize() => 8;
    }
}