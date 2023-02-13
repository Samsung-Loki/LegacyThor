// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using TheAirBlow.Thor.Enigma.Exceptions;

namespace TheAirBlow.Thor.Enigma.Receivers;

public class StringReceiver : IReceiver {
    public string Received;
    
    /// <summary>
    /// Receive the string
    /// </summary>
    /// <param name="buf">Byte Buffer</param>
    public void Receive(byte[] buf)
        => Received = Encoding.ASCII.GetString(buf);
}