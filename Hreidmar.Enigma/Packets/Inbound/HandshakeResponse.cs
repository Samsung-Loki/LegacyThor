using System;
using System.IO;
using System.Text;
using Spectre.Console;

namespace Hreidmar.Enigma.Packets.Inbound
{
    public class HandshakeResponse : IInboundPacket
    {
        public void Unpack(byte[] buf)
        {
            using var memory = new MemoryStream(buf);
            using var stream = new BinaryReader(memory);
            var str = Encoding.ASCII.GetString(stream.ReadBytes(4));
            if (str != "LOKE")
                throw new Exception($"Invalid response: {str}");
        }

        public int GetSize() => 4;
    }
}