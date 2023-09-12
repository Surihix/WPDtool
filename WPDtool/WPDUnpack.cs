using BinaryReaderEx;
using BinaryWriterEx;
using IMGBClasses;
using StreamExtension;
using System;
using System.IO;
using System.Linq;
using System.Text;
using WPDtool.SupportClasses;

namespace WPDtool.WPDClasses
{
    internal partial class WPD
    {
        public static void UnpackWPD(string inWPDfile)
        {
            var wpdFileName = Path.GetFileName(inWPDfile);
            var wpdFileDir = Path.GetDirectoryName(inWPDfile);
            var inWPDimgbFile = Path.Combine(wpdFileDir, Path.GetFileNameWithoutExtension(inWPDfile) + ".imgb");

            var extractWpdDir = Path.Combine(wpdFileDir, "_" + wpdFileName);
            var extractImgbDir = Path.Combine(Path.GetDirectoryName(inWPDfile), "_" + inWPDimgbFile);

            DeleteDirIfExists(extractWpdDir);
            Directory.CreateDirectory(extractWpdDir);

            if (File.Exists(inWPDimgbFile))
            {
                DeleteDirIfExists(extractImgbDir);
                Directory.CreateDirectory(extractImgbDir);
            }


            using (var wpdStream = new FileStream(inWPDfile, FileMode.Open, FileAccess.Read))
            {
                using (var wpdReader = new BinaryReader(wpdStream))
                {
                    wpdReader.BaseStream.Position = 0;
                    var wpdChars = wpdReader.ReadBytes(4);
                    var wpdHeader = Encoding.ASCII.GetString(wpdChars).Replace("\0", "");

                    if (!wpdHeader.Equals("WPD"))
                    {
                        CmnMethods.ErrorExit("Error: Not a valid WPD file");
                    }

                    wpdReader.BaseStream.Position = 4;
                    var totalRecords = wpdReader.ReadBytesUInt32(true);

                    Console.WriteLine("Writing record list....");
                    WriteRecordList(totalRecords, wpdReader, extractWpdDir);
                    Console.WriteLine("");


                    uint readStartPos = 16;
                    for (int f = 0; f < totalRecords; f++)
                    {
                        wpdReader.BaseStream.Position = readStartPos;
                        var currentRecordName = wpdReader.ReadStringTillNull();

                        wpdReader.BaseStream.Position = readStartPos + 16;
                        var currentRecordStart = wpdReader.ReadBytesUInt32(true);

                        wpdReader.BaseStream.Position = readStartPos + 20;
                        var currentRecordSize = wpdReader.ReadBytesUInt32(true);

                        wpdReader.BaseStream.Position = readStartPos + 24;
                        var currentRecordExtension = "." + wpdReader.ReadStringTillNull();
                        currentRecordExtension = currentRecordExtension == "." ? "" : currentRecordExtension;

                        var currentOutFile = Path.Combine(extractWpdDir, currentRecordName + currentRecordExtension);
                        Console.WriteLine("Extracted " + currentOutFile);

                        using (var ofs = new FileStream(currentOutFile, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            wpdStream.ExCopyTo(ofs, currentRecordStart, currentRecordSize);
                        }

                        if (IMGB.ImgHeaderBlockFileExtensions.Contains(currentRecordExtension))
                        {
                            if (File.Exists(inWPDimgbFile))
                            {
                                IMGB.UnpackIMGB(currentOutFile, inWPDimgbFile, extractImgbDir);
                            }
                        }

                        Console.WriteLine("");
                        readStartPos += 32;
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Finished extracting " + inWPDfile);
            Console.ReadLine();
        }


        static void DeleteDirIfExists(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, true);
            }
        }


        static void WriteRecordList(uint totalRecords, BinaryReader readerName, string extractWpdDir)
        {
            using (var fs = new FileStream(Path.Combine(extractWpdDir, "!!recordsList_Nova"), FileMode.Append, FileAccess.Write))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.BaseStream.Position = 0;
                    bw.WriteBytesUInt32(totalRecords, false);

                    using (var sw = new StreamWriter(fs))
                    {

                        uint readStartPos = 16;
                        for (int r = 0; r < totalRecords; r++)
                        {
                            readerName.BaseStream.Position = readStartPos;
                            sw.Write(readerName.ReadStringTillNull());

                            readerName.BaseStream.Position = readStartPos + 24;
                            var extn = "\0" + readerName.ReadStringTillNull();

                            if (!extn.Equals("\0"))
                            {
                                sw.Write(extn);
                            }
                            else
                            {
                                sw.Write("\0.");
                            }

                            sw.Write("\0");

                            readStartPos += 32;
                        }
                    }
                }
            }
        }
    }
}