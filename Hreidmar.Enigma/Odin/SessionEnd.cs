// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace Hreidmar.Enigma.Odin;

/// <summary>
/// SessionEnd Commands
/// </summary>
public enum SessionEnd
{
    /// <summary>
    /// End current session
    /// </summary>
    EndSession = 0x00,
    
    /// <summary>
    /// Reboot the device
    /// </summary>
    Reboot = 0x01,
    
    /// <summary>
    /// Reboot into ODIN
    /// </summary>
    OdinReboot = 0x02,
    
    /// <summary>
    /// Shutdown the device
    /// </summary>
    Shutdown = 0x03
}