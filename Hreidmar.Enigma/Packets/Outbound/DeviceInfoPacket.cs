using System.IO;
using System.Text;

namespace Hreidmar.Enigma.Packets.Outbound
{
    public class DeviceInfoPacket : IOutboundPacket
    {
        public byte[] Pack()
        {
            var buf = new byte[4];
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryWriter(memory);
            stream.Write(Encoding.ASCII.GetBytes("DVIF"));
            return memory.ToArray();
        }
        
        public int GetSize() => 4;
    }
}