// ReSharper disable InconsistentNaming
namespace Hreidmar.Enigma.Packets
{
    /// <summary>
    /// Packet type. First 32-bit number in the request.
    /// </summary>
    public enum PacketType
    {
        Pit = 0x65,
        File = 0x66,
        Session = 0x64,
        EndSession = 0x67
    }

    /// <summary>
    /// Shared packet commands. Second 32-bit number in the request.
    /// </summary>
    public enum SharedCommands
    {
        Dump = 0x01,
        End = 0x03,
        Flash = 0x00,
        /// <summary>
        /// Also may be begin.
        /// </summary>
        FilePart = 0x02
    }

    /// <summary>
    /// Session packet commands. Second 32-bit number in the request.
    /// </summary>
    public enum SessionCommands
    {
        Begin = 0x00,
        TFlash = 0x08,
        /// <summary>
        /// Always returns 30. Does something different now.
        /// </summary>
        DeviceType = 0x01,
        TotalBytes = 0x02,
        FilePartSize = 0x05,
        EraseUserData = 0x07,
        PrintSalesCode = 0x09
    }

    /// <summary>
    /// End session packet commands. Second 32-bit number in the request.
    /// </summary>
    public enum EndSessionCommands
    {
        Shutdown = 0x03,
        Reboot = 0x01,
        End = 0x00
    }

    /// <summary>
    /// ODIN protocol version.
    /// </summary>
    public enum ProtocolVersion
    {
        Version4 = 0x04,
        Version3 = 0x03
    }
}