// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Hreidmar.Enigma.Senders;

/// <summary>
/// Send a byte array
/// </summary>
public class ByteSender : ISender
{
    public byte[] Send()
        => _buffer;
    
    private byte[] _buffer;

    public ByteSender(byte[] buffer)
        => _buffer = buffer;
}