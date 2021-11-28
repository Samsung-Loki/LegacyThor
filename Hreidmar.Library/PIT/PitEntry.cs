using System;

namespace Hreidmar.Library.PIT
{
    /// <summary>
    /// A PIT entry
    /// </summary>
    public class PitEntry
    {
        public enum BinaryTypeEnum {
            AP = 0,
            BL = 1,
            Unknown
        }
        
        public enum DeviceTypeEnum {
            NAND = 0,
            File = 1,
            MMC = 2,
            All = 3,
            Unknown
        }
        
        [Flags]
        public enum AttributeEnum {
            FOTA = 0x0001,
            Secure = 0x0010
        }

        public BinaryTypeEnum BinaryType;
        public DeviceTypeEnum DeviceType;
        public AttributeEnum Attributes;
        public AttributeEnum UpdateAttributes;
        public int Identifier;
        public int BlockSizeOrOffset;
        public int BlockCount;
        public int FileOffset;
        public int FileSize;
        public string PartitionName;
        public string FlashName;
        public string FotaName;
    }
}