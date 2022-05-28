// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Thor.Enigma.PIT;

/// <summary>
/// Attributes
/// </summary>
public enum Attributes
{
    // V1 only
    ReadOnly = 0,
    ReadWrite = 1,
    STL = 2,
    
    // V2 only
    None = 0,
    BCT = 1,
    Bootloader = 2,
    PartitionTable = 3,
    NVData = 4,
    Data = 5,
    MBR = 6,
    EBR = 7,
    GP1 = 8 | 9
}