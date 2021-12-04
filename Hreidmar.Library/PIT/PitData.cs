using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hreidmar.Library.Exceptions;

namespace Hreidmar.Library.PIT
{
    /// <summary>
    /// PIT file data
    /// </summary>
    public class PitData
    {
        public readonly List<PitEntry> Entries = new();
        public byte[] OriginalFile;

        public static PitData FromFile(string filename)
        {
            var data = new PitData {
                OriginalFile = File.ReadAllBytes(filename)
            };
            using var file = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(file);
            if (reader.ReadInt32() != 0x12349876)
                throw new InvalidPitFileException("Invalid file identifier!");
            var count = reader.ReadInt32();
            reader.BaseStream.Seek(4 * 8, SeekOrigin.Current);
            for (int i = 0; i < count; i++)
            {
                reader.BaseStream.Seek(28 + i * 132, SeekOrigin.Begin);
                var entry = new PitEntry {
                    BinaryType = (PitEntry.BinaryTypeEnum)reader.ReadInt32(),
                    DeviceType = (PitEntry.DeviceTypeEnum)reader.ReadInt32(),
                    Identifier = reader.ReadInt32(),
                    Attributes = (PitEntry.AttributeEnum)reader.ReadInt32(),
                    UpdateAttributes = (PitEntry.AttributeEnum)reader.ReadInt32(),
                    BlockSizeOrOffset = reader.ReadInt32(),
                    BlockCount = reader.ReadInt32(),
                    FileOffset = reader.ReadInt32(),
                    FileSize = reader.ReadInt32()
                };
                var buf = new byte[32];
                reader.Read(buf, 0, buf.Length);
                entry.PartitionName = Encoding.ASCII.GetString(buf).TrimEnd('\0');
                buf = new byte[32];
                reader.Read(buf, 0, buf.Length);
                entry.FlashName = Encoding.ASCII.GetString(buf).TrimEnd('\0');
                buf = new byte[32];
                reader.Read(buf, 0, buf.Length);
                entry.FotaName = Encoding.ASCII.GetString(buf).TrimEnd('\0');
                data.Entries.Add(entry);
            }

            return data;
        }

        public static PitData FromBytes(byte[] buffer)
        {
            var data = new PitData {
                OriginalFile = buffer
            };
            using var memory = new MemoryStream(buffer);
            using var reader = new BinaryReader(memory);
            if (reader.ReadInt32() != 0x12349876)
                throw new InvalidPitFileException("Invalid file identifier!");
            var count = reader.ReadInt32();
            reader.BaseStream.Seek(4 * 8, SeekOrigin.Current);
            for (int i = 0; i < count; i++)
            {
                reader.BaseStream.Seek(28 + i * 132, SeekOrigin.Begin);
                var entry = new PitEntry {
                    BinaryType = (PitEntry.BinaryTypeEnum)reader.ReadInt32(),
                    DeviceType = (PitEntry.DeviceTypeEnum)reader.ReadInt32(),
                    Identifier = reader.ReadInt32(),
                    Attributes = (PitEntry.AttributeEnum)reader.ReadInt32(),
                    UpdateAttributes = (PitEntry.AttributeEnum)reader.ReadInt32(),
                    BlockSizeOrOffset = reader.ReadInt32(),
                    BlockCount = reader.ReadInt32(),
                    FileOffset = reader.ReadInt32(),
                    FileSize = reader.ReadInt32()
                };
                var buf = new byte[32];
                reader.Read(buf, 0, buf.Length);
                entry.PartitionName = Encoding.ASCII.GetString(buf).TrimEnd('\0');
                buf = new byte[32];
                reader.Read(buf, 0, buf.Length);
                entry.FlashName = Encoding.ASCII.GetString(buf).TrimEnd('\0');
                buf = new byte[32];
                reader.Read(buf, 0, buf.Length);
                entry.FotaName = Encoding.ASCII.GetString(buf).TrimEnd('\0');
                data.Entries.Add(entry);
            }

            return data;
        }
    }
}