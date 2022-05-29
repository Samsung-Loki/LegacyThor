// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using TheAirBlow.Thor.Enigma.Exceptions;

namespace TheAirBlow.Thor.Enigma.Receivers;

/// <summary>
/// Byte Ack
/// </summary>
public class ByteAck : IReceiver
{
    /// <summary>
    /// Receive byte buffer
    /// </summary>
    /// <param name="buf">Byte Buffer</param>
    public void Receive(byte[] buf)
    {
        using var memory = new MemoryStream(buf);
        using var reader = new BinaryReader(memory);
        var value1 = reader.ReadInt32();
        if (value1 != _expectedValue)
            throw new UnexpectedValueException(
                $"Received 0x{value1:X4}, expected 0x{_expectedValue:X4}");

        var value2 = reader.ReadInt32();
        if (value2 != 0)
            throw new UnexpectedValueException(
                $"Received 0x{value1:X4}, expected zero!");
    }

    /// <summary>
    /// Expected value
    /// </summary>
    private int _expectedValue;

    /// <summary>
    /// Byte Ack
    /// </summary>
    /// <param name="packetType">Packet Type</param>
    public ByteAck(int packetType)
        => _expectedValue = packetType;
}