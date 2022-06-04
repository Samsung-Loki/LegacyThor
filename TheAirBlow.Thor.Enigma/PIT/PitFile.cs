// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TheAirBlow.Thor.Enigma.PIT;

/// <summary>
/// A PIT file
/// </summary>
public class PitFile
{
    /// <summary>
    /// Header
    /// </summary>
    public PitHeader Header = new();
    
    /// <summary>
    /// Partition entries
    /// </summary>
    public List<PitEntry> Entries = new();
    
    /// <summary>
    /// Is it version 2 PIT file?
    /// </summary>
    public bool IsVersion2 = false;

    /// <summary>
    /// Parse a PIT file from raw byte data
    /// </summary>
    /// <param name="data">Raw byte data</param>
    public PitFile(byte[] data)
    {
        using var memory = new MemoryStream(data);
        Parse(memory);
    }

    /// <summary>
    /// Parse a PIT file from file
    /// </summary>
    /// <param name="path">File Path</param>
    public PitFile(string path)
    {
        using var file = new FileStream(path, FileMode.Open);
        Parse(file);
    }

    /// <summary>
    /// Parse a PIT file from stream
    /// </summary>
    /// <param name="stream">Stream</param>
    public PitFile(Stream stream)
        => Parse(stream);

    /// <summary>
    /// Create an empty PIT file
    /// </summary>
    public PitFile() {}

    /// <summary>
    /// Pack a PIT file into a raw byte buffer
    /// </summary>
    /// <returns>Raw byte buffer</returns>
    public byte[] Pack()
    {
        using var stream = new MemoryStream();
        Pack(stream); return stream.ToArray();
    }

    /// <summary>
    /// Pack the PIT file into stream
    /// </summary>
    /// <param name="input">Stream</param>
    public void Pack(Stream input)
    {
        using var stream = new BinaryWriter(input);
        stream.Write(0x12349876);
        stream.Write(Header.PartitionsCount);
        stream.Write(Encoding.UTF8.GetBytes(Header.GangName));
        stream.Write(Encoding.UTF8.GetBytes(Header.ProjectName));
        stream.Write(Header.Dummy);
        foreach (var i in Entries) {
            stream.Write((int)i.BinaryType);
            stream.Write((int)i.DeviceType);
            stream.Write(i.Identifier);
            stream.Write((int)i.Attributes);
            stream.Write((int)i.UpdateAttributes);
            stream.Write(i.StartBlockOrCount);
            stream.Write(i.BlockCountOrNumber);
            stream.Write(i.FileOffset);
            stream.Write(i.FileSize);
            stream.Write(Encoding.UTF8.GetBytes(i.Name));
            stream.Write(Encoding.UTF8.GetBytes(i.FileName));
            stream.Write(Encoding.UTF8.GetBytes(i.DeltaName));
        }
    }

    /// <summary>
    /// Checks if the PIT is V1 or V2
    /// </summary>
    public void DetectVersion()
    {
        var last = -1;
        foreach (var i in Entries) {
            if (last == -1) {
                last = i.StartBlockOrCount;
                continue;
            }

            if (last != i.StartBlockOrCount) {
                IsVersion2 = true;
                break;
            }
            last = i.StartBlockOrCount;
        }
    }
    
    /// <summary>
    /// Parse a PIT file from stream
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <exception cref="InvalidDataException">Invalid magic number</exception>
    private void Parse(Stream stream)
    {
        using (var reader = new BinaryReader(stream)) {
            var magic = reader.ReadInt32();
            if (magic != 0x12349876)
                throw new InvalidDataException($"Expected 0x12349876 magic number, got {magic:X2}");
            Header.PartitionsCount = reader.ReadInt32();
            Header.GangName = Encoding.UTF8.GetString(reader.ReadBytes(8)).Replace("\0", "");
            Header.ProjectName = Encoding.UTF8.GetString(reader.ReadBytes(8)).Replace("\0", "");
            Header.Dummy = reader.ReadInt32();
            for (var i = 0; i < Header.PartitionsCount; i++)
                Entries.Add(new PitEntry {
                    BinaryType = (BinaryType)reader.ReadInt32(),
                    DeviceType = (DeviceType)reader.ReadInt32(),
                    Identifier = reader.ReadInt32(),
                    Attributes = (Attributes)reader.ReadInt32(),
                    UpdateAttributes = (UpdateAttributes)reader.ReadInt32(),
                    StartBlockOrCount = reader.ReadInt32(),
                    BlockCountOrNumber = reader.ReadInt32(),
                    FileOffset = reader.ReadInt32(),
                    FileSize = reader.ReadInt32(),
                    Name = Encoding.UTF8.GetString(reader.ReadBytes(32)).Replace("\0", ""),
                    FileName = Encoding.UTF8.GetString(reader.ReadBytes(32)).Replace("\0", ""),
                    DeltaName = Encoding.UTF8.GetString(reader.ReadBytes(32)).Replace("\0", "")
                });
        }

        // Check the PIT file version
        DetectVersion();

        // Convert V1 to V2 if needed
        foreach (var i in Entries) {
            if ((int) i.DeviceType == 2) 
                i.DeviceType = IsVersion2 ? DeviceType.EMMC : DeviceType.MOVINAND;

            if (!IsVersion2) continue;
            switch (i.Attributes) {
                case Attributes.ReadOnly:
                    i.Attributes = Attributes.None;
                    break;
                case Attributes.ReadWrite:
                    i.Attributes = Attributes.BCT;
                    break;
                case Attributes.STL:
                    i.Attributes = Attributes.Bootloader;
                    break;
            }
            switch (i.UpdateAttributes) {
                case UpdateAttributes.FOTA:
                    i.UpdateAttributes = UpdateAttributes.Basic;
                    break;
                case UpdateAttributes.Secure:
                    i.UpdateAttributes = UpdateAttributes.Enhanced;
                    break;
                case UpdateAttributes.SecureFOTA:
                    i.UpdateAttributes = UpdateAttributes.EXT2;
                    break;
            }
        }
    }
}
