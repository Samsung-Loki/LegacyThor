// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Thor.Enigma.Senders;

/// <summary>
/// Transmitter sender
/// </summary>
public interface ISender
{
    /// <summary>
    /// Send a byte buffer
    /// </summary>
    /// <returns>Byte buffer</returns>
    public byte[] Send();
}