// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Hreidmar.Enigma.PIT;

/// <summary>
/// Update (FOTA) attrubutes
/// </summary>
public enum UpdateAttributes
{
    // V1 only
    None = 0,
    FOTA = 1,
    Secure = 2,
    SecureFOTA = 3,
    
    // V2 only
    Basic = 1,
    Enhanced = 2,
    EXT2 = 3,
    YAFFS2 = 4,
    EXT4 = 5
}