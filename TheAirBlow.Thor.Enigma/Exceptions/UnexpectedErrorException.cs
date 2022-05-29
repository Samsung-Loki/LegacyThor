// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace TheAirBlow.Thor.Enigma.Exceptions;

internal class UnexpectedErrorException : Exception
{
    public UnexpectedErrorException(string message) : base(message) { }
}