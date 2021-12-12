namespace Hreidmar.Enigma.Packets.Outbound
{
    public interface IOutboundPacket
    {
        public byte[] Pack();
        public int GetSize();
    }
}