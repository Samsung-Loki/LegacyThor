using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Spectre.Console;

namespace Hreidmar.Enigma.Packets.Inbound
{
    public class DeviceInfoResponse : IInboundPacket
    {
        public Dictionary<string, string> Information = new();

        public void Unpack(byte[] buf)
        {
            File.WriteAllBytes("test.bin", buf);
            var str = Encoding.ASCII.GetString(buf);
            var data = str.Substring(2, str.LastIndexOf("@#", StringComparison.Ordinal) - 2);
            foreach (var pair in data.Split(';'))
                Information.Add(pair.Split('=')[0], pair.Split('=')[1]);
        }

        public int GetSize() => 1024;
    }
}
