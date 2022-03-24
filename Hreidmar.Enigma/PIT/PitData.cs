using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hreidmar.Enigma.Exceptions;

namespace Hreidmar.Enigma.PIT
{
    /// <summary>
    /// PIT file data
    /// </summary>
    public class PitData
    {
        /// <summary>
        /// PIT partition entries
        /// </summary>
        // ReSharper disable once CollectionNeverQueried.Global
        public readonly List<PitEntry> Entries = new();
        
        /// <summary>
        /// Original file's content
        /// </summary>
        public byte[] OriginalFile;

        /// <summary>
        /// Load PIT from file
        /// </summary>
        /// <param name="filename">File Path</param>
        /// <returns>PitData Instance</returns>
        public static PitData FromFile(string filename)
        {
            using var file = new FileStream(filename, FileMode.Open, FileAccess.Read);
            return FromStream(file);
        }

        /// <summary>
        /// Load PIT from byte buffer
        /// </summary>
        /// <param name="buffer">Byte Buffer</param>
        /// <returns>PitData Intance</returns>
        public static PitData FromBytes(byte[] buffer)
        {
            using var memory = new MemoryStream(buffer);
            return FromStream(memory);
        }
        
        /// <summary>
        /// Load PIT from stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>PitData Instance</returns>
        public static PitData FromStream(Stream stream)
        {
            var data = new PitData();
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != 0x12349876)
                throw new InvalidPitFileException("Invalid file identifier!");
            var count = reader.ReadInt32();
            data.OriginalFile = reader.ReadBytes((int)stream.Length);
            stream.Seek(0, SeekOrigin.Begin);
            reader.BaseStream.Seek(4 * 8, SeekOrigin.Current);
            for (int i = 0; i < count; i++)
            {
                reader.BaseStream.Seek(28 + i * 132, SeekOrigin.Begin);
                var entry = new PitEntry {
                    BinaryType = (PitEntry.BinaryTypeEnum)reader.ReadInt32(),
                    DeviceType = (PitEntry.DeviceTypeEnum)reader.ReadInt32(),
                    Identifier = reader.ReadInt32(),
                    Attributes = (PitEntry.AttributeEnum)reader.ReadInt32(),
                    UpdateAttributes = (PitEntry.UpdateAttributeEnum)reader.ReadInt32(),
                    BlockSize = reader.ReadInt32(),
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