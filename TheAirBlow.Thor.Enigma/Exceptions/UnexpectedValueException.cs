// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace TheAirBlow.Thor.Enigma.Exceptions;

public class UnexpectedValueException : Exception
{
    public UnexpectedValueException()
    {
    }

    public UnexpectedValueException(string msg)
        : base(msg)
    {
    }

    public UnexpectedValueException(string msg, Exception inner)
        : base(msg, inner)
    {
    }
}