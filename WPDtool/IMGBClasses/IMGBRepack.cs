using BinaryReaderEx;
using System;
using System.IO;
using System.Linq;
using WPDtool.SupportClasses;

namespace WPDtool.IMGBClasses
{
    internal partial class IMGB
    {
        public static void RepackIMGBType1(string imgHeaderBlockFile, string outImgbFile, string extractedImgbDir)
        {
            var gtexPos = GetGTEXChunkPos(imgHeaderBlockFile);
            if (gtexPos == 0)
            {
                Console.WriteLine("Unable to find GTEX chunk. skipped to next file.");
                return;
            }

            var imgbVars = new IMGB();
            imgbVars.GTEXStartVal = gtexPos;

            GetImageInfo(imgHeaderBlockFile, imgbVars);

            if (!ImgFormatValuesArray.Contains(imgbVars.ImgFormatValue))
            {
                Console.WriteLine("Detected unknown image format. skipped to next file.");
                return;
            }

            if (!ImgTypeValuesArray.Contains(imgbVars.ImgTypeValue))
            {
                Console.WriteLine("Detected unknown image type. skipped to next file.");
                return;
            }


            // Open the IMGB file and start extracting
            // the images according to the image type
            using (var imgbStream = new FileStream(outImgbFile, FileMode.Open, FileAccess.Write))
            {

                switch (imgbVars.ImgTypeValue)
                {
                    // Classic or Other type
                    // Type 0 is Classic
                    // Type 4 is Other
                    case 0:
                    case 4:
                        RepackClassicType(imgHeaderBlockFile, extractedImgbDir, imgbVars, imgbStream);
                        break;

                    // Cubemap type 
                    // Type 5 is for PS3
                    case 1:
                    case 5:
                        RepackCubemapType(imgHeaderBlockFile, extractedImgbDir, imgbVars, imgbStream);
                        break;

                    // Stacked type (LR only)
                    // PC version wpd may or may not use
                    // this type.
                    case 2:
                        if (imgbVars.ImgMipCount > 1)
                        {
                            Console.WriteLine("Detected more than one mip in this stack type image. skipped to next file.");
                            return;
                        }
                        RepackStackType(imgHeaderBlockFile, extractedImgbDir, imgbVars, imgbStream);
                        break;
                }
            }
        }



        // Classic type
        static void RepackClassicType(string imgHeaderBlockFile, string extractImgbDir, IMGB imgbVars, FileStream imgbStream)
        {
            var imgHeaderBlockFileName = Path.GetFileName(imgHeaderBlockFile);
            var currentDDSfile = Path.Combine(extractImgbDir, imgHeaderBlockFileName + ".dds");

            if (!File.Exists(currentDDSfile))
            {
                Console.WriteLine("Missing image file. skipped to next file.");
                return;
            }

            using (var gtexStream = new FileStream(imgHeaderBlockFile, FileMode.Open, FileAccess.Read))
            {
                using (var gtexReader = new BinaryReader(gtexStream))
                {
                    gtexReader.BaseStream.Position = imgbVars.GTEXStartVal + 16;
                    var mipOffsetsStartPos = gtexReader.ReadBytesUInt32(true);

                    using (FileStream ddsStream = new FileStream(currentDDSfile, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader ddsReader = new BinaryReader(ddsStream))
                        {
                            GetExtImgInfo(ddsReader, imgbVars);
                            var isValidImg = CheckExtImgInfo(imgbVars);

                            if (!isValidImg)
                            {
                                return;
                            }

                            using (MemoryStream tempDdsStream = new MemoryStream())
                            {
                                ddsStream.Seek(128, SeekOrigin.Begin);
                                ddsStream.CopyTo(tempDdsStream);


                                uint mipStart = 0;
                                uint nextMipStart = 0;
                                uint totalMipSize = 0;
                                uint readStart = imgbVars.GTEXStartVal + mipOffsetsStartPos;

                                for (int m = 0; m < imgbVars.OutImgMipCount; m++)
                                {
                                    gtexReader.BaseStream.Position = readStart;
                                    var copyTextureAt = gtexReader.ReadBytesUInt32(true);

                                    gtexReader.BaseStream.Position = readStart + 4;
                                    var mipSize = gtexReader.ReadBytesUInt32(true);

                                    imgbStream.Seek(copyTextureAt, SeekOrigin.Begin);
                                    tempDdsStream.ExCopyTo(imgbStream, mipStart, mipSize);

                                    gtexReader.BaseStream.Position = readStart + 8;
                                    readStart = (uint)gtexReader.BaseStream.Position;

                                    nextMipStart = mipSize + totalMipSize;
                                    mipStart = nextMipStart;
                                    totalMipSize = nextMipStart;
                                }
                            }
                        }
                    }

                    Console.WriteLine("Repacked " + currentDDSfile + " data to IMGB.");
                }
            }
        }



        // Cubemap type
        static void RepackCubemapType(string imgHeaderBlockFile, string extractImgbDir, IMGB imgbVars, FileStream imgbStream)
        {
            var imgHeaderBlockFileName = Path.GetFileName(imgHeaderBlockFile);

            var isMissingAnImg = CheckImgFilesBatch(6, extractImgbDir, imgHeaderBlockFileName, imgbVars);
            if (isMissingAnImg)
            {
                Console.WriteLine("Missing one or more cubemap type image files. skipped to next file.");
                return;
            }

            var isAllValidImg = CheckExtImgInfoBatch(6, extractImgbDir, imgHeaderBlockFileName, imgbVars);
            if (!isAllValidImg)
            {
                return;
            }

            using (var gtexStream = new FileStream(imgHeaderBlockFile, FileMode.Open, FileAccess.Read))
            {
                using (var gtexReader = new BinaryReader(gtexStream))
                {

                    int cubeMapCount = 1;
                    uint readStart = 0;
                    bool file1 = true;

                    for (int c = 0; c < 6; c++)
                    {
                        var currentDDSfile = Path.Combine(extractImgbDir, imgHeaderBlockFileName + imgbVars.ImgType + cubeMapCount + ".dds");

                        using (FileStream ddsCbMapStream = new FileStream(currentDDSfile, FileMode.Open, FileAccess.Read))
                        {
                            using (MemoryStream tempDdsStream = new MemoryStream())
                            {
                                ddsCbMapStream.Seek(128, SeekOrigin.Begin);
                                ddsCbMapStream.CopyTo(tempDdsStream);


                                uint mipStart = 0;
                                uint nextMipStart = 0;
                                uint totalMipSize = 0;

                                if (file1)
                                {
                                    readStart = imgbVars.GTEXStartVal + 24;
                                }

                                for (int m = 0; m < imgbVars.ImgMipCount; m++)
                                {
                                    gtexReader.BaseStream.Position = readStart;
                                    uint copyTextureAt = gtexReader.ReadBytesUInt32(true);

                                    gtexReader.BaseStream.Position = readStart + 4;
                                    uint mipSize = gtexReader.ReadBytesUInt32(true);

                                    imgbStream.Seek(copyTextureAt, SeekOrigin.Begin);
                                    tempDdsStream.ExCopyTo(imgbStream, mipStart, mipSize);

                                    gtexReader.BaseStream.Position = readStart + 8;
                                    readStart = (uint)gtexReader.BaseStream.Position;

                                    nextMipStart = mipSize + totalMipSize;
                                    mipStart = nextMipStart;
                                    totalMipSize = nextMipStart;
                                }
                            }
                        }

                        file1 = false;

                        Console.WriteLine("Repacked " + currentDDSfile + " data to IMGB.");

                        cubeMapCount++;
                    }
                }
            }
        }



        // Stack type
        static void RepackStackType(string extractImgbDir, string imgHeaderBlockFile, IMGB imgbVars, FileStream imgbStream)
        {
            var imgHeaderBlockFileName = Path.GetFileName(imgHeaderBlockFile);

            var isMissingAnImg = CheckImgFilesBatch(imgbVars.ImgDepth, extractImgbDir, imgHeaderBlockFileName, imgbVars);
            if (isMissingAnImg)
            {
                Console.WriteLine("Missing one or more stack type image files. skipped to next file.");
                return;
            }

            var isAllValidImg = CheckExtImgInfoBatch(imgbVars.ImgDepth, extractImgbDir, imgHeaderBlockFileName, imgbVars);
            if (!isAllValidImg)
            {
                return;
            }

            using (var gtexStream = new FileStream(imgHeaderBlockFile, FileMode.Open, FileAccess.Read))
            {
                using (var gtexReader = new BinaryReader(gtexStream))
                {
                    gtexReader.BaseStream.Position = imgbVars.GTEXStartVal + 16;
                    var mipOffsetsStartPos = gtexReader.ReadBytesUInt32(true);

                    var mipOffsetsReadStartPos = imgbVars.GTEXStartVal + mipOffsetsStartPos;

                    gtexReader.BaseStream.Position = mipOffsetsReadStartPos;
                    var copyTextureAt = gtexReader.ReadBytesUInt32(true);

                    gtexReader.BaseStream.Position = mipOffsetsReadStartPos + 4;
                    var mipSize = gtexReader.ReadBytesUInt32(true);


                    int stackCount = 1;
                    uint mipStart = 0;

                    for (int s = 0; s < imgbVars.ImgDepth; s++)
                    {
                        var currentDDSfile = Path.Combine(extractImgbDir, imgHeaderBlockFileName + imgbVars.ImgType + stackCount + ".dds");

                        using (var ddsStackStream = new FileStream(currentDDSfile, FileMode.Open, FileAccess.Read))
                        {
                            using (var stackReader = new BinaryReader(ddsStackStream))
                            {
                                GetExtImgInfo(stackReader, imgbVars);
                                var isValidImg = CheckExtImgInfo(imgbVars);

                                if (!isValidImg)
                                {
                                    return;
                                }

                                using (var tempDDSstream = new MemoryStream())
                                {
                                    ddsStackStream.Seek(128, SeekOrigin.Begin);
                                    ddsStackStream.CopyTo(tempDDSstream);

                                    imgbStream.Seek(copyTextureAt, SeekOrigin.Begin);
                                    tempDDSstream.ExCopyTo(imgbStream, mipStart, mipSize);

                                    var NextStackTxtrStart = mipStart + mipSize;
                                    mipStart = NextStackTxtrStart;
                                }
                            }
                        }

                        Console.WriteLine("Repacked " + currentDDSfile + " data to IMGB.");

                        stackCount++;
                    }
                }
            }
        }



        // Repack methods
        static bool CheckExtImgInfo(IMGB imgbVars)
        {
            var isValidImg = true;
            if (imgbVars.ImgMipCount != imgbVars.OutImgMipCount)
            {
                Console.WriteLine("Current image's mip count does not match the original image's mip count. skipped to next file.");
                isValidImg = false;
            }

            if (imgbVars.ImgWidth != imgbVars.OutImgWidth)
            {
                Console.WriteLine("Current image's width does not match the original image's width. skipped to next file.");
                isValidImg = false;
            }

            if (imgbVars.ImgHeight != imgbVars.OutImgHeight)
            {
                Console.WriteLine("Current image's height does not match the original image's height. skipped to next file.");
                isValidImg = false;
            }

            if (imgbVars.ImgFormatValue != imgbVars.OutImgFormatValue)
            {
                Console.WriteLine("Detected unknown image file format. skipped to next file.");
                isValidImg = false;
            }

            return isValidImg;
        }


        static bool CheckImgFilesBatch(int fileAmount, string extractImgbDir, string imgHeaderBlockFileName, IMGB imgbVars)
        {
            var isMissingAnImg = false;
            var imgFileCount = 1;

            for (int i = 0; i < fileAmount; i++)
            {
                var fileToCheck = Path.Combine(extractImgbDir, imgHeaderBlockFileName + imgbVars.ImgType + imgFileCount + ".dds");

                if (!File.Exists(fileToCheck))
                {
                    isMissingAnImg = true;
                }

                imgFileCount++;
            }

            return isMissingAnImg;
        }


        static bool CheckExtImgInfoBatch(int fileAmount, string extractImgbDir, string imgHeaderBlockFileName, IMGB imgbVars)
        {
            var isAllValidImg = true;
            var imgFileCount = 1;

            for (int i = 0; i < fileAmount; i++)
            {
                var fileToCheck = Path.Combine(extractImgbDir, imgHeaderBlockFileName + imgbVars.ImgType + imgFileCount + ".dds");

                using (var ddsFileToCheck = new FileStream(fileToCheck, FileMode.Open, FileAccess.Read))
                {
                    using (var ddsFileReader = new BinaryReader(ddsFileToCheck))
                    {
                        GetExtImgInfo(ddsFileReader, imgbVars);
                        var isValidImg = CheckExtImgInfo(imgbVars);

                        if (!isValidImg)
                        {
                            isAllValidImg = false;
                        }
                    }
                }

                imgFileCount++;
            }

            return isAllValidImg;
        }
    }
}