// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Hreidmar.Enigma.PIT;

/// <summary>
/// Flash storage type
/// </summary>
public enum DeviceType
{
    // Shared between V1/V2
    ONENAND = 0,
    NAND = 1,
    
    // Shared, but mean different things
    MOVINAND = 2,
    EMMC = 2,
    
    // V2 only
    SPI = 3,
    IDE = 4,
    NAND_X16 = 5
}