// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using TheAirBlow.Thor.Enigma.Exceptions;

namespace TheAirBlow.Thor.Enigma.Receivers;

public class StringAck : IReceiver
{
    /// <summary>
    /// Receive the string
    /// </summary>
    /// <param name="buf">Byte Buffer</param>
    public void Receive(byte[] buf)
    {
        var str = Encoding.UTF8.GetString(buf);
        if (str != _expectedValue)
            throw new UnexpectedValueException(
                $"Received {buf}, expected {_expectedValue}");
    }

    /// <summary>
    /// Expected value
    /// </summary>
    private string _expectedValue;

    /// <summary>
    /// Expected value (shorthand)
    /// </summary>
    /// <param name="expectedValue">Expected value</param>
    public StringAck(string expectedValue)
        => _expectedValue = expectedValue;
}