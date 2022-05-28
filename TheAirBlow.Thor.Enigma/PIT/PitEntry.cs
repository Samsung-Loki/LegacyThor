// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace TheAirBlow.Thor.Enigma.PIT;

/// <summary>
/// PIT partition entry
/// </summary>
public class PitEntry
{
    /// <summary>
    /// Binary Type (AP/CP)
    /// </summary>
    public BinaryType BinaryType;
    
    /// <summary>
    /// Flash memory type
    /// </summary>
    public DeviceType DeviceType;
    
    /// <summary>
    /// Partition identifier
    /// </summary>
    public int Identifier;
    
    /// <summary>
    /// Attributes
    /// </summary>
    public Attributes Attributes;
    
    /// <summary>
    /// Update (FOTA) attributes
    /// </summary>
    public UpdateAttributes UpdateAttributes;

    /// <summary>
    /// Start block or Block Count
    /// </summary>
    public int StartBlockOrCount;
    
    /// <summary>
    /// Count of blocks or Block Number
    /// </summary>
    public int BlockCountOrNumber = -1;

    /// <summary>
    /// File Offset (obsolete)
    /// </summary>
    public int FileOffset;
    
    /// <summary>
    /// File Size (obsolete)
    /// </summary>
    public int FileSize;
    
    /// <summary>
    /// Partition Name
    /// </summary>
    public string Name;
    
    /// <summary>
    /// File Name in firmware
    /// </summary>
    public string FileName;
    
    /// <summary>
    /// Delta (FOTA) name, usually used for "remainder"
    /// </summary>
    public string DeltaName;
}