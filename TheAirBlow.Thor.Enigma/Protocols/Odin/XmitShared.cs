// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Thor.Enigma.Protocols.Odin;

/// <summary>
/// XMIT Commands
/// </summary>
public enum XmitShared
{
    /// <summary>
    /// Request a flash operation
    /// </summary>
    RequestFlash = 0x00,
    
    /// <summary>
    /// Request a dump operation
    /// </summary>
    RequestDump = 0x01,
    
    /// <summary>
    /// Begin operation
    /// </summary>
    Begin = 0x02,
    
    /// <summary>
    /// End operaiton
    /// </summary>
    End = 0x03,
    
    /// <summary>
    /// Requst a compressed flash operation
    /// </summary>
    RequestCompressedFlash = 0x05,
    
    /// <summary>
    /// Begin compressed operation
    /// </summary>
    BeginCompressed = 0x06,
    
    /// <summary>
    /// End compressed operation
    /// </summary>
    EndCompressed = 0x07
}