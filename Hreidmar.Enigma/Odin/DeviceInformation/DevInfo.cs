// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Hreidmar.Enigma.Exceptions;

namespace Hreidmar.Enigma.Odin.DeviceInformation;

/// <summary>
/// DevInfo format
/// </summary>
public class DevInfo
{
    /// <summary>
    /// DevInfo items
    /// </summary>
    public List<Item> Items = new();

    /// <summary>
    /// Original file's content
    /// </summary>
    public byte[] OriginalFile;
    
    /// <summary>
    /// DevInfo item's type
    /// </summary>
    public enum Type
    {
        /// <summary>
        /// Model's name
        /// </summary>
        ModelName = 0x00,
        
        /// <summary>
        /// Serial Code
        /// </summary>
        SerialCode = 0x01,
        
        /// <summary>
        /// Sales/region code
        /// </summary>
        SalesCode = 0x02,
        
        /// <summary>
        /// Carrier ID
        /// </summary>
        CarrierId = 0x03
    }

    /// <summary>
    /// A DevInfo Item
    /// </summary>
    public class Item
    {
        public Type Type;
        public string Value;
    }

    /// <summary>
    /// Parse DevInfo from stream
    /// </summary>
    /// <param name="stream">Stream</param>
    /// <returns>DevInfo Instance</returns>
    public static DevInfo FromStream(Stream stream)
    {
        var data = new DevInfo();
        using var reader = new BinaryReader(stream);
        data.OriginalFile = reader.ReadBytes((int)stream.Length);
        stream.Seek(0, SeekOrigin.Begin);
        if (reader.ReadInt32() != 0x12345678)
            throw new InvalidDevInfoException("Invalid magic number!");
        var count = reader.ReadInt32();
        
        // Skip the locations
        stream.Seek(8 + 12 * count,
            SeekOrigin.Begin);
        
        // Read the items themselves
        for (var i = 0; i < count; i++) {
            var type = (Type)reader.ReadInt32();
            var size = reader.ReadInt32();
            var str = Encoding.UTF8.GetString(
                reader.ReadBytes(size));
            data.Items.Add(new Item {
                Type = type,
                Value = str
            });
        }

        return data;
    }
}