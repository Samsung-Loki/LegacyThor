// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;

namespace TheAirBlow.Thor.Enigma.Senders;

public abstract class StringSender : ISender
{
    /// <summary>
    /// Send the string
    /// </summary>
    /// <returns></returns>
    public byte[] Send()
        => Encoding.UTF8.GetBytes(_str);

    /// <summary>
    /// String to send
    /// </summary>
    private string _str;

    /// <summary>
    /// Send a string
    /// </summary>
    /// <param name="str">String</param>
    public StringSender(string str)
        => _str = str;
}