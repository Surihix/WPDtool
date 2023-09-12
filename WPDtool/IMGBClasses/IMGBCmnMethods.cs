using BinaryReaderEx;
using System;
using System.IO;
using System.Text;

namespace WPDtool.IMGBClasses
{
    internal partial class IMGB
    {
        static uint GetGTEXChunkPos(string inImgHeaderBlockFile)
        {
            uint gtexPos = 0;
            var gtexChunkString = "GTEX";
            var gtexChunkStringArray = new byte[4];
            var imgHeaderBlockFileData = File.ReadAllBytes(inImgHeaderBlockFile);

            for (int g = 0; g < imgHeaderBlockFileData.Length; g++)
            {
                if ((char)imgHeaderBlockFileData[g] == gtexChunkString[0])
                {
                    Buffer.BlockCopy(imgHeaderBlockFileData, g, gtexChunkStringArray, 0, 4);
                    var gtex = Encoding.ASCII.GetString(gtexChunkStringArray, 0, 4);

                    if (gtex == gtexChunkString)
                    {
                        gtexPos = (uint)g;
                        break;
                    }
                }
            }

            return gtexPos;
        }


        static void GetImageInfo(string inImgHeaderBlockFile, IMGB imgbVars)
        {
            using (var gtexStream = new FileStream(inImgHeaderBlockFile, FileMode.Open, FileAccess.Read))
            {
                using (var gtexReader = new BinaryReader(gtexStream))
                {
                    gtexReader.BaseStream.Position = imgbVars.GTEXStartVal + 6;
                    imgbVars.ImgFormatValue = gtexReader.ReadByte();
                    imgbVars.ImgMipCount = gtexReader.ReadByte();

                    imgbVars.ImgMipCount = imgbVars.ImgMipCount.Equals(0) ? (byte)1 : imgbVars.ImgMipCount;

                    gtexReader.BaseStream.Position = imgbVars.GTEXStartVal + 9;
                    imgbVars.ImgTypeValue = gtexReader.ReadByte();
                    imgbVars.ImgWidth = gtexReader.ReadBytesUInt16(true);
                    imgbVars.ImgHeight = gtexReader.ReadBytesUInt16(true);
                    imgbVars.ImgDepth = gtexReader.ReadBytesUInt16(true);

                    switch (imgbVars.ImgTypeValue)
                    {
                        case 1:
                        case 5:
                            imgbVars.ImgType = "_cbmap_";
                            break;

                        case 2:
                            imgbVars.ImgType = "_stack_";
                            break;
                    }
                }
            }
        }


        static byte[] MortonUnswizzle(IMGB imgbVars, byte[] swizzledBufferVar)
        {
            int widthVar = imgbVars.ImgWidth;
            int heightVar = imgbVars.ImgHeight;

            var unswizzledBufferVar = new byte[widthVar * heightVar * 4];
            var processBufferVar = new byte[4];

            var arrayReadPos = 0;
            for (int m = 0; m < widthVar * heightVar; m++)
            {
                Array.Copy(swizzledBufferVar, arrayReadPos, processBufferVar, 0, 4);

                int val1 = 0;
                int val2 = 0;
                int val3;
                int val4 = (val3 = 1);
                int val5 = m;
                int val6 = widthVar;
                int val7 = heightVar;

                while (val6 > 1 || val7 > 1)
                {
                    if (val6 > 1)
                    {
                        val1 += val4 * (val5 & 1);
                        val5 >>= 1;
                        val4 *= 2;
                        val6 >>= 1;
                    }
                    if (val7 > 1)
                    {
                        val2 += val3 * (val5 & 1);
                        val5 >>= 1;
                        val3 *= 2;
                        val7 >>= 1;
                    }
                }

                var processedPixel = val2 * widthVar + val1;
                int pixelOffset = processedPixel * 4;

                Array.Copy(processBufferVar, 0, unswizzledBufferVar, pixelOffset, 4);

                arrayReadPos += 4;
            }

            return unswizzledBufferVar;
        }


        static byte[] ColorAsBGRA(byte[] unSwizzledBufferVar)
        {
            var correctedColors = new byte[unSwizzledBufferVar.Length];

            using (MemoryStream unSwizzledStream = new MemoryStream())
            {
                unSwizzledStream.Write(unSwizzledBufferVar, 0, unSwizzledBufferVar.Length);
                unSwizzledStream.Seek(0, SeekOrigin.Begin);

                using (BinaryReader unSwizzledReader = new BinaryReader(unSwizzledStream))
                {

                    using (MemoryStream adjustedColorStream = new MemoryStream())
                    {
                        adjustedColorStream.Write(unSwizzledBufferVar, 0, unSwizzledBufferVar.Length);
                        adjustedColorStream.Seek(0, SeekOrigin.Begin);

                        using (BinaryWriter adjustedColorWriter = new BinaryWriter(adjustedColorStream))
                        {

                            var readPos = 0;
                            var writePos = 0;

                            for (int p = 0; p < unSwizzledBufferVar.Length; p++)
                            {
                                unSwizzledReader.BaseStream.Position = readPos;
                                var alpha = unSwizzledReader.ReadByte();
                                var red = unSwizzledReader.ReadByte();
                                var green = unSwizzledReader.ReadByte();
                                var blue = unSwizzledReader.ReadByte();

                                adjustedColorWriter.BaseStream.Position = writePos;
                                adjustedColorWriter.Write(blue);
                                adjustedColorWriter.Write(green);
                                adjustedColorWriter.Write(red);
                                adjustedColorWriter.Write(alpha);

                                if (readPos < (unSwizzledBufferVar.Length - 4))
                                {
                                    readPos += 4;
                                    writePos += 4;
                                }
                            }

                            adjustedColorStream.Seek(0, SeekOrigin.Begin);
                            adjustedColorStream.Read(correctedColors, 0, correctedColors.Length);
                        }
                    }
                }
            }

            return correctedColors;
        }


        static void GetExtImgInfo(BinaryReader ddsReader, IMGB imgbVars)
        {
            ddsReader.BaseStream.Position = 12;
            imgbVars.OutImgHeight = ddsReader.ReadUInt32();
            imgbVars.OutImgWidth = ddsReader.ReadUInt32();

            ddsReader.BaseStream.Position = 28;
            imgbVars.OutImgMipCount = ddsReader.ReadUInt32();

            ddsReader.BaseStream.Position = 84;
            var getImgFormat = ddsReader.ReadChars(4);
            var imgFormatString = string.Join("", getImgFormat).Replace("\0", "");

            switch (imgFormatString)
            {
                case "":
                    if (imgbVars.ImgFormatValue == 3)
                    {
                        imgbVars.OutImgFormatValue = 3;
                    }
                    if (imgbVars.ImgFormatValue == 4)
                    {
                        imgbVars.OutImgFormatValue = 4;
                    }
                    break;

                case "DXT1":
                    imgbVars.OutImgFormatValue = 24;
                    break;

                case "DXT3":
                    imgbVars.OutImgFormatValue = 25;
                    break;

                case "DXT5":
                    imgbVars.OutImgFormatValue = 26;
                    break;

                default:
                    imgbVars.OutImgFormatValue = 0;
                    break;
            }
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
    }
}