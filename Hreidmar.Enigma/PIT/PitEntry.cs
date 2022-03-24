using System;

namespace Hreidmar.Enigma.PIT
{
    /// <summary>
    /// A PIT entry
    /// </summary>
    public class PitEntry
    {
        public enum BinaryTypeEnum {
            AP = 0,
            CP = 1,
            Unknown
        }
        
        public enum DeviceTypeEnum {
            NAND = 1,
            EMMC = 2,
            SPI = 3,
            IDE = 4,
            NANDX16 = 5,
            NOR = 6,
            NANDWB1 = 7,
            UFS = 8,
            Unknown
        }
        
        [Flags]
        public enum AttributeEnum {
            ReadOnly = 0,
            ReadWrite = 1,
            STL = 3
        }
        
        [Flags]
        public enum UpdateAttributeEnum {
            None = 0,
            Fota = 1,
            Secure = 2,
            FotaSecure = 3
        }

        public BinaryTypeEnum BinaryType;
        public DeviceTypeEnum DeviceType;
        public AttributeEnum Attributes;
        public UpdateAttributeEnum UpdateAttributes;
        public int Identifier;
        public int BlockSize;
        public int BlockCount;
        public int FileOffset;
        public int FileSize;
        public string PartitionName;
        public string FlashName;
        public string FotaName;
    }
}