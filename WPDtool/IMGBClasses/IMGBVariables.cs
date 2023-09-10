namespace WPDtool.IMGBClasses
{
    internal partial class IMGB
    {
        public static readonly string[] ImgHeaderBlockFileExtensions = new string[]
        {
            ".txbh",
            ".txb",
            ".vtex"
        };

        public uint GTEXStartVal { get; set; }

        public byte ImgFormatValue { get; set; }
        public static readonly byte[] ImgFormatValuesArray = new byte[]
        {
            3, 4, 24, 25, 26
        };

        public byte ImgMipCount { get; set; }

        public byte ImgTypeValue { get; set; }
        public static readonly byte[] ImgTypeValuesArray = new byte[]
        {
            0, 4, 1, 5, 2
        };

        public string ImgType { get; set; }

        public ushort ImgWidth { get; set; }

        public ushort ImgHeight { get; set; }

        public ushort ImgDepth { get; set; }

        public bool IsPs3Imgb { get; set; }

        public bool IsX360Imgb { get; set; }

        public uint OutImgWidth { get; set; }

        public uint OutImgHeight { get; set; }

        public uint OutImgMipCount { get; set; }
        
        public byte OutImgFormatValue { get; set; }
    }
}