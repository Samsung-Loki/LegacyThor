namespace Hreidmar.Library.Packets.Outbound
{
    public interface IOutboundPacket
    {
        public byte[] Pack();
        public int GetSize();
    }
}