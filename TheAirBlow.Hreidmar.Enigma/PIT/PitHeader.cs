// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Hreidmar.Enigma.PIT;

/// <summary>
/// PIT file header
/// </summary>
public class PitHeader
{
    /// <summary>
    /// Amount of Partitions
    /// </summary>
    public int PartitionsCount;
    
    /// <summary>
    /// GANG Name (usually COM_TAR2)
    /// </summary>
    public string GangName;
    
    /// <summary>
    /// Project Name (SDM710, SoC ID)
    /// </summary>
    public string ProjectName;
    
    /// <summary>
    /// Protocol Version or LUN count
    /// </summary>
    public int Dummy;
}