// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.IO;
using System.Text;

namespace TheAirBlow.Thor.Enigma;

public class DeviceInfo
{
    public string Model;
    public string Region;
    public string CarrierID;
    public string SerialCode;
    
    public DeviceInfo() {}
    
    public DeviceInfo(byte[] buf)
    {
        using var memory = new MemoryStream(buf);
        using var stream = new BinaryReader(memory);
        var magic = stream.ReadInt32();
        if (magic != 0x12345678)
            throw new InvalidDataException($"Expected 0x12345678 magic number, got {magic:X2}");
        var count = stream.ReadInt32();
        
        // Skip locations
        for (var i = 0; i < count; i++) {
            stream.ReadInt32();
            stream.ReadInt32();
            stream.ReadInt32();
        }

        for (var i = 0; i < count; i++) {
            var type = stream.ReadInt32();
            var size = stream.ReadInt32();
            var str = Encoding.ASCII.GetString(
                stream.ReadBytes(size));
            switch (type) {
                case 0x00:
                    Model = str;
                    break;
                case 0x01:
                    SerialCode = str;
                    break;
                case 0x02:
                    Region = str;
                    break;
                case 0x03:
                    CarrierID = str;
                    break;
            }
        }
    }
}