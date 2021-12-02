using System;
using System.Linq;

namespace Hreidmar.Library.Packets
{
    public interface IOutboundPacket
    {
        public byte[] Pack();
        public int GetSize();
    }
}