// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Thor.Enigma.Protocols.Odin;

/// <summary>
/// SessionStart Commands
/// </summary>
public enum SessionStart
{
    /// <summary>
    /// Begin session
    /// </summary>
    BeginSession = 0x00,
    
    /// <summary>
    /// Reset flash counter
    /// </summary>
    ResetFlashCounter = 0x01,
    
    /// <summary>
    /// Set Total Bytes
    /// </summary>
    TotalBytes = 0x02,
    
    /// <summary>
    /// OEM State (Unknown)
    /// </summary>
    OemState = 0x03,
    
    /// <summary>
    /// No OEM Check (Unknown)
    /// </summary>
    NoOemCheck = 0x04,
    
    /// <summary>
    /// Set File Part size
    /// </summary>
    FilePartSize = 0x05,
    
    /// <summary>
    /// Do a factory reset
    /// </summary>
    EraseUserdata = 0x07,
    
    /// <summary>
    /// Enable T-Flash
    /// </summary>
    EnableTFlash = 0x08,
    
    /// <summary>
    /// Set sales/region code
    /// </summary>
    SetRegionCode = 0x09,
    
    /// <summary>
    /// Enable RTN config on boot
    /// </summary>
    EnableRtn = 0x0A
}