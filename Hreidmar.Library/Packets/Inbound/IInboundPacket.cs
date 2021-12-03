namespace Hreidmar.Library.Packets.Inbound
{
    public interface IInboundPacket
    {
        public void Unpack(byte[] buf);
        public int GetSize();
    }
}