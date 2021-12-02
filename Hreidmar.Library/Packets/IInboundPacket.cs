using System;
using System.Linq;

namespace Hreidmar.Library.Packets
{
    public interface IInboundPacket
    {
        public void Unpack(byte[] buf);
        public int GetSize();
    }
}