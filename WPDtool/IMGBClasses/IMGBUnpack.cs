using BinaryReaderEx;
using System;
using System.IO;
using System.Linq;
using WPDtool.SupportClasses;

namespace WPDtool.IMGBClasses
{
    internal partial class IMGB
    {
        public static void UnpackIMGB(string imgHeaderBlockFile, string inImgbFile, string extractIMGBdir)
        {
            var gtexPos = GetGTEXChunkPos(imgHeaderBlockFile);
            if (gtexPos == 0)
            {
                Console.WriteLine("Unable to find GTEX chunk. skipped to next file.");
                return;
            }

            var imgbVars = new IMGB();
            imgbVars.GTEXStartVal = gtexPos;
            imgbVars.IsPs3Imgb = inImgbFile.EndsWith("ps3.imgb");
            imgbVars.IsX360Imgb = inImgbFile.EndsWith("x360.imgb");

            if (imgbVars.IsX360Imgb)
            {
                Console.WriteLine("Detected x360 version imgb file. images may not extract correctly.");
            }

            GetImageInfo(imgHeaderBlockFile, imgbVars);

            Console.WriteLine("Image Format Value: " + imgbVars.ImgFormatValue);
            Console.WriteLine("Image MipCount: " + imgbVars.ImgMipCount);
            Console.WriteLine("Image Type Value: " + imgbVars.ImgTypeValue);
            Console.WriteLine("Image Width: " + imgbVars.ImgWidth);
            Console.WriteLine("Image Height: " + imgbVars.ImgHeight);

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
            using (var imgbStream = new FileStream(inImgbFile, FileMode.Open, FileAccess.ReadWrite))
            {

                switch (imgbVars.ImgTypeValue)
                {
                    // Classic or Other type
                    // Type 0 is Classic
                    // Type 4 is Other
                    case 0:
                    case 4:
                        UnpackClassic(imgHeaderBlockFile, extractIMGBdir, imgbVars, imgbStream);
                        break;

                    // Cubemap type 
                    // Type 5 is for PS3
                    case 1:
                    case 5:
                        UnpackCubemap(imgHeaderBlockFile, extractIMGBdir, imgbVars, imgbStream);
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
                        UnpackStack(imgHeaderBlockFile, extractIMGBdir, imgbVars, imgbStream);
                        break;
                }
            }
        }


        static void UnpackClassic(string imgHeaderBlockFile, string extractIMGBdir, IMGB imgbVars, FileStream imgbStream)
        {
            var imgHeaderBlockFileName = Path.GetFileName(imgHeaderBlockFile);

            using (var gtexStream = new FileStream(imgHeaderBlockFile, FileMode.Open, FileAccess.Read))
            {
                using (var gtexReader = new BinaryReader(gtexStream))
                {
                    var currentDDSfile = Path.Combine(extractIMGBdir, imgHeaderBlockFileName + ".dds");

                    using (var ddsStream = new FileStream(currentDDSfile, FileMode.Append, FileAccess.Write))
                    {
                        using (var ddsWriter = new BinaryWriter(ddsStream))
                        {
                            BaseHeader(ddsStream, ddsWriter, imgbVars);
                            PixelFormatHeader(ddsWriter, imgbVars);

                            gtexReader.BaseStream.Position = imgbVars.GTEXStartVal + 16;
                            var mipOffsetsStartPos = gtexReader.ReadBytesUInt32(true);


                            uint mipOffsetsReadStartPos = imgbVars.GTEXStartVal + mipOffsetsStartPos;
                            for (int m = 0; m < imgbVars.ImgMipCount; m++)
                            {
                                gtexReader.BaseStream.Position = mipOffsetsReadStartPos;
                                var mipStart = gtexReader.ReadBytesUInt32(true);

                                gtexReader.BaseStream.Position = mipOffsetsReadStartPos + 4;
                                var mipSize = gtexReader.ReadBytesUInt32(true);

                                imgbStream.Seek(mipStart, SeekOrigin.Begin);
                                var writeMipDataAt = ddsStream.Length;
                                ddsStream.Seek(writeMipDataAt, SeekOrigin.Begin);

                                // Set a bool to indicate whether to copy 
                                // dds data or not
                                var doneCopying = false;

                                // If the conditions match a swizzled ps3 texture,
                                // then unswizzle the texture data, color correct the data,
                                // and copy the unswizzled texture data to the final dds file. 
                                if (imgbVars.IsPs3Imgb && imgbVars.ImgFormatValue.Equals(4) && imgbVars.ImgTypeValue.Equals(4))
                                {
                                    var swizzledArray = new byte[mipSize];
                                    imgbStream.Read(swizzledArray, 0, swizzledArray.Length);

                                    var unSwizzledArray = MortonUnswizzle(imgbVars, swizzledArray);
                                    var correctedColorArray = ColorAsBGRA(unSwizzledArray);

                                    ddsStream.Write(correctedColorArray, 0, correctedColorArray.Length);
                                    doneCopying = true;
                                }

                                // If the conditions match a ps3 pixel format 4 texture without
                                // the swizzle type flag, then color correct the data and copy
                                // the data to the final dds file.
                                if (imgbVars.IsPs3Imgb && imgbVars.ImgFormatValue.Equals(4) && imgbVars.ImgTypeValue.Equals(0))
                                {
                                    var colorDataToCorrectArray = new byte[mipSize];
                                    imgbStream.Read(colorDataToCorrectArray, 0, colorDataToCorrectArray.Length);

                                    var correctedColorArray = ColorAsBGRA(colorDataToCorrectArray);

                                    ddsStream.Write(correctedColorArray, 0, correctedColorArray.Length);
                                    doneCopying = true;
                                }

                                // If the conditions match a ps3 pixel format 3 texture,
                                // then color correct the data and copy the data to the 
                                // final dds file.
                                if (imgbVars.IsPs3Imgb && imgbVars.ImgFormatValue.Equals(3))
                                {
                                    var colorDataToCorrectArray = new byte[mipSize];
                                    imgbStream.Read(colorDataToCorrectArray, 0, colorDataToCorrectArray.Length);

                                    var correctedColorArray = ColorAsBGRA(colorDataToCorrectArray);

                                    ddsStream.Write(correctedColorArray, 0, correctedColorArray.Length);
                                    doneCopying = true;
                                }

                                // If the condition matches a win32 texture file or a file
                                // that does not need anything specific done, then copy the data
                                // directly to the final dds file.
                                if (doneCopying.Equals(false))
                                {
                                    imgbStream.ExCopyTo(ddsStream, mipStart, mipSize);
                                }

                                mipOffsetsReadStartPos += 8;
                            }
                        }
                    }

                    Console.WriteLine("Extracted " + currentDDSfile);
                }
            }
        }


        static void UnpackCubemap(string imgHeaderBlockFile, string extractIMGBdir, IMGB imgbVars, FileStream imgbStream)
        {
            var imgHeaderBlockFileName = Path.GetFileName(imgHeaderBlockFile);

            using (var gtexStream = new FileStream(imgHeaderBlockFile, FileMode.Open, FileAccess.Read))
            {
                using (var gtexReader = new BinaryReader(gtexStream))
                {
                    gtexReader.BaseStream.Position = imgbVars.GTEXStartVal + 16;
                    var mipOffsetsStartPos = gtexReader.ReadBytesUInt32(true);

                    uint mipOffsetsReadStartPos = imgbVars.GTEXStartVal + mipOffsetsStartPos;


                    int cubeMapCount = 1;
                    for (int cb = 0; cb < 6; cb++)
                    {
                        var currentDDSfile = Path.Combine(extractIMGBdir, imgHeaderBlockFileName + imgbVars.ImgType + cubeMapCount + ".dds");

                        using (var ddsStream = new FileStream(currentDDSfile, FileMode.Append, FileAccess.Write))
                        {
                            using (var ddsWriter = new BinaryWriter(ddsStream))
                            {
                                BaseHeader(ddsStream, ddsWriter, imgbVars);
                                PixelFormatHeader(ddsWriter, imgbVars);

                                for (int m = 0; m < imgbVars.ImgMipCount; m++)
                                {
                                    gtexReader.BaseStream.Position = mipOffsetsReadStartPos;
                                    var mipStart = gtexReader.ReadBytesUInt32(true);

                                    gtexReader.BaseStream.Position = mipOffsetsReadStartPos + 4;
                                    var mipSize = gtexReader.ReadBytesUInt32(true);

                                    var writeMipDataAt = ddsStream.Length;
                                    ddsStream.Seek(writeMipDataAt, SeekOrigin.Begin);

                                    imgbStream.ExCopyTo(ddsStream, mipStart, mipSize);

                                    mipOffsetsReadStartPos += 8;
                                }
                            }
                        }

                        Console.WriteLine("Extracted " + currentDDSfile);

                        cubeMapCount++;

                        gtexReader.BaseStream.Position = mipOffsetsReadStartPos;
                        mipOffsetsReadStartPos = (uint)gtexReader.BaseStream.Position;
                    }
                }
            }
        }


        static void UnpackStack(string imgHeaderBlockFile, string extractIMGBdir, IMGB imgbVars, FileStream imgbStream)
        {
            var imgHeaderBlockFileName = Path.GetFileName(imgHeaderBlockFile);

            using (var gtexStream = new FileStream(imgHeaderBlockFile, FileMode.Open, FileAccess.Read))
            {
                using (var gtexReader = new BinaryReader(gtexStream))
                {
                    gtexReader.BaseStream.Position = imgbVars.GTEXStartVal + 16;
                    var mipOffsetsStartPos = gtexReader.ReadBytesUInt32(true);

                    var mipOffsetsReadStartPos = imgbVars.GTEXStartVal + mipOffsetsStartPos;

                    gtexReader.BaseStream.Position = mipOffsetsReadStartPos;
                    var mipStart = gtexReader.ReadBytesUInt32(true);

                    gtexReader.BaseStream.Position = mipOffsetsReadStartPos + 4;
                    var mipSize = gtexReader.ReadBytesUInt32(true);


                    int stackCount = 1;
                    mipSize /= 4;
                    for (int st = 0; st < imgbVars.ImgDepth; st++)
                    {
                        var currentDDSfile = Path.Combine(extractIMGBdir, imgHeaderBlockFileName + imgbVars.ImgType + stackCount + ".dds");

                        using (var ddsStream = new FileStream(currentDDSfile, FileMode.Append, FileAccess.Write))
                        {
                            using (var ddsWriter = new BinaryWriter(ddsStream))
                            {
                                BaseHeader(ddsStream, ddsWriter, imgbVars);
                                PixelFormatHeader(ddsWriter, imgbVars);

                                var writeMipDataAt = ddsStream.Length;
                                ddsStream.Seek(writeMipDataAt, SeekOrigin.Begin);

                                imgbStream.ExCopyTo(ddsStream, mipStart, mipSize);

                                var NextStackTxtrStart = mipStart + mipSize;
                                mipStart = NextStackTxtrStart;
                            }
                        }

                        Console.WriteLine("Extracted " + currentDDSfile);

                        stackCount++;
                    }
                }
            }
        }
    }
}