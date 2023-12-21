using BinaryReaderEx;
using BinaryWriterEx;
using IMGBlibrary;
using StreamExtension;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WPDtool
{
    internal partial class WPD
    {
        public static void UnpackWPD(string inWPDfile)
        {
            var wpdFileName = Path.GetFileName(inWPDfile);
            var wpdFileDir = Path.GetDirectoryName(inWPDfile);
            var inWPDimgbFile = Path.Combine(wpdFileDir, Path.GetFileNameWithoutExtension(inWPDfile) + ".imgb");

            var extractWPDdir = Path.Combine(wpdFileDir, "_" + wpdFileName);
            var extractIMGBdir = Path.Combine(Path.GetDirectoryName(inWPDfile), "_" + Path.GetFileName(inWPDimgbFile));

            DeleteDirIfExists(extractWPDdir);
            Directory.CreateDirectory(extractWPDdir);

            if (File.Exists(inWPDimgbFile))
            {
                DeleteDirIfExists(extractIMGBdir);
                Directory.CreateDirectory(extractIMGBdir);
            }


            Console.WriteLine("");

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
                    WriteRecordList(totalRecords, wpdReader, extractWPDdir);
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

                        var currentOutFile = Path.Combine(extractWPDdir, currentRecordName + currentRecordExtension);
                        Console.WriteLine("Extracted " + currentOutFile);

                        using (var ofs = new FileStream(currentOutFile, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            wpdStream.ExCopyTo(ofs, currentRecordStart, currentRecordSize);
                        }

                        if (IMGBVariables.ImgHeaderBlockExtns.Contains(currentRecordExtension))
                        {
                            if (File.Exists(inWPDimgbFile))
                            {
                                IMGBUnpack.UnpackIMGB(currentOutFile, inWPDimgbFile, extractIMGBdir);
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
            using (var fs = new FileStream(Path.Combine(extractWpdDir, CmnMethods.RecordsList), FileMode.Append, FileAccess.Write))
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