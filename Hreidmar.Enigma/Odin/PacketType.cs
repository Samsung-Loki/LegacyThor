// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace Hreidmar.Enigma.Odin;

/// <summary>
/// Packet type
/// </summary>
public enum PacketType
{
    /// <summary>
    /// Session control
    /// </summary>
    SessionStart = 0x64,
    
    /// <summary>
    /// PIT flashing/dumping
    /// </summary>
    PitXmit = 0x65,
    
    /// <summary>
    /// File flashing
    /// </summary>
    FileXmit = 0x66,

    /// <summary>
    /// End session
    /// </summary>
    SessionEnd = 0x67,
 
    /// <summary>
    /// Device Information
    /// </summary>
    DeviceInfo = 0x69
}