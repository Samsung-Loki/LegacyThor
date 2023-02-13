using LibUsbDotNet;
using TheAirBlow.Thor.Enigma.Receivers;
using TheAirBlow.Thor.Enigma.Senders;

namespace TheAirBlow.Thor.Enigma.Protocols; 

/// <summary>
/// PROMPT console
/// </summary>
public class PromptProtocol : Protocol {
    /// <summary>
    /// Create a new instance of PromptProtocol
    /// </summary>
    /// <param name="writer">Writer</param>
    /// <param name="reader">Reader</param>
    public PromptProtocol(UsbEndpointWriter writer, 
        UsbEndpointReader reader) : base(writer, reader) { }
    
    /// <summary>
    /// Send PROMPT command
    /// </summary>
    /// <param name="cmd">Command</param>
    /// <returns>Bootloader's response</returns>
    public string SendCmd(string cmd) {
        var rec = (StringReceiver)Send(new StringSender($"PROMPT{cmd}"), new StringReceiver());
        return rec.Received;
    }

    /// <summary>
    /// Does nothing, as the prompt console
    /// acts like a handshake by itself.
    /// </summary>
    public override bool Handshake() {
        return true;
    }
}