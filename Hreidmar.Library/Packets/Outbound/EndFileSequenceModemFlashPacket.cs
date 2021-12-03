using System.IO;
using Hreidmar.Library.PIT;

namespace Hreidmar.Library.Packets.Outbound
{
    /// <summary>
    /// End file sequence flash: MODEM/CP
    /// </summary>
    public class EndFileSequenceModemFlashPacket : IOutboundPacket
    {
        public int Length;
        public int Identifier;
        public bool IsLastSequence;
        public PitEntry.DeviceTypeEnum DeviceType;

        public byte[] Pack()
        {
            var buf = new byte[1024];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(0x66);                   // File type
            stream.Write(0x01);                   // Modem flag
            stream.Write(Length);                 // Size
            stream.Write(0x00);                   // Unknown
            stream.Write((int)DeviceType);        // Device Type
            stream.Write(Identifier);             // Identifier
            stream.Write(IsLastSequence ? 1 : 0); // Is last sequence
            return memory.ToArray();
        }
        
        public int GetSize() => 1024;
    }
}