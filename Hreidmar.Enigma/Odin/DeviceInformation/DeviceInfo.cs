// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace Hreidmar.Enigma.Odin.DeviceInformation;

/// <summary>
/// DeviceInfo Commands
/// </summary>
public enum DeviceInfo
{
    /// <summary>
    /// Begin DevInfo dump
    /// </summary>
    BeginDump = 0x00,
    
    /// <summary>
    /// Dump a block
    /// </summary>
    DumpBlock = 0x01,
    
    /// <summary>
    /// End DevInfo dump
    /// </summary>
    EndDump = 0x02
}