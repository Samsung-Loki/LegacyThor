// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using LibUsbDotNet;
using TheAirBlow.Thor.Enigma.Receivers;
using TheAirBlow.Thor.Enigma.Senders;

namespace TheAirBlow.Thor.Enigma.Protocols;

/// <summary>
/// Odin v2/v3 protocol
/// </summary>
public class OdinProtocol : Protocol
{
    /// <summary>
    /// Create a new instance of OdinProtocol
    /// </summary>
    /// <param name="writer">Writer</param>
    /// <param name="reader">Reader</param>
    public OdinProtocol(UsbEndpointWriter writer, 
        UsbEndpointReader reader) : base(writer, reader) { }

    /// <summary>
    /// Do a handshake
    /// </summary>
    public override bool Handshake()
    {
        var rec = Send(new StringSender("ODIN"), 
            new StringAck("LOKE"));
        return rec != null;
    }
}