// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using TheAirBlow.Hreidmar.Enigma.Receivers;
using TheAirBlow.Hreidmar.Enigma.Senders;

namespace TheAirBlow.Hreidmar.Enigma;

/// <summary>
/// Message transmitter
/// </summary>
public class Transmitter
{
    private IReceiver _receiver;
    private ISender _sender;

    /// <summary>
    /// Set receiver to use
    /// </summary>
    /// <param name="receiver">Receiver</param>
    public void SetReceiver(IReceiver receiver)
        => _receiver = receiver;

    /// <summary>
    /// Set sender to use
    /// </summary>
    /// <param name="sender">Sender</param>
    public void SetSender(ISender sender)
        => _sender = sender;

    /// <summary>
    /// Get the receiver
    /// </summary>
    /// <returns>Receiver</returns>
    internal IReceiver GetReceiver()
        => _receiver;

    /// <summary>
    /// Get the sender
    /// </summary>
    /// <returns>Sender</returns>
    internal ISender GetSender()
        => _sender;
}