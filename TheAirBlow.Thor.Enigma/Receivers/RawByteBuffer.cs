// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Thor.Enigma.Receivers;

public class RawByteBuffer : IReceiver
{
    public byte[] Data;
    
    /// <summary>
    /// Receive byte buffer
    /// </summary>
    /// <param name="buf">Byte Buffer</param>
    public void Receive(byte[] buf)
        => Data = buf;
}