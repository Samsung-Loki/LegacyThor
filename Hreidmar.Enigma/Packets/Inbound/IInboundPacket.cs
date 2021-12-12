namespace Hreidmar.Enigma.Packets.Inbound
{
    public interface IInboundPacket
    {
        public void Unpack(byte[] buf);
        public int GetSize();
    }
}