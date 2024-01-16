using BinaryReaderEx;
using BinaryWriterEx;
using IMGBlibrary;
using StreamExtension;
using System;
using System.IO;
using System.Linq;

namespace WPDtool
{
    internal partial class WPD
    {
        public static void RepackWPD(string inWPDExtractedDir)
        {
            var inWPDExtractedDirRoot = Path.GetDirectoryName(inWPDExtractedDir);

            var outWPDfileName = Path.GetFileName(inWPDExtractedDir);
            outWPDfileName = outWPDfileName.Remove(0, 1);

            var outWPDfile = Path.Combine(inWPDExtractedDirRoot, outWPDfileName);
            var outWPDImgbFile = Path.Combine(inWPDExtractedDirRoot, Path.GetFileNameWithoutExtension(outWPDfileName) + ".imgb");
            var inWPDExtractedIMGBDir = Path.Combine(inWPDExtractedDirRoot, "_" + Path.GetFileNameWithoutExtension(outWPDfileName) + ".imgb");

            var recordsListFile = Path.Combine(inWPDExtractedDir, CmnMethods.RecordsList);

            if (!File.Exists(recordsListFile))
            {
                CmnMethods.ErrorExit($"Error: Missing file '{CmnMethods.RecordsList}' in extracted directory. Please ensure that the wpd file is unpacked properly with this tool.");
            }

            if (Directory.Exists(inWPDExtractedIMGBDir))
            {
                if (Directory.GetFiles(inWPDExtractedIMGBDir).Length != 0)
                {
                    if (outWPDImgbFile.EndsWith("ps3.imgb") || outWPDImgbFile.EndsWith("x360.imgb"))
                    {
                        CmnMethods.ErrorExit("Error: Detected PS3 or Xbox 360 version's extracted IMGB directory. repacking is not supported for these two versions.");
                    }

                    if (!File.Exists(outWPDImgbFile))
                    {
                        CmnMethods.ErrorExit($"Error: Paired imgb file for the extracted IMGB directory, is missing.\nPlease ensure that the paired imgb file is present next to the extracted imgb directory.");
                    }
                }
            }

            if (File.Exists(outWPDfile))
            {
                IfFileExistsDel(outWPDfile + ".old");

                File.Move(outWPDfile, outWPDfile + ".old");
            }

            if (File.Exists(outWPDImgbFile))
            {
                IfFileExistsDel(outWPDImgbFile + ".old");

                File.Copy(outWPDImgbFile, outWPDImgbFile + ".old");
            }


            using (var recordsList = new FileStream(recordsListFile, FileMode.Open, FileAccess.Read))
            {
                using (var recordsListReader = new BinaryReader(recordsList))
                {
                    recordsListReader.BaseStream.Position = 0;
                    var totalRecords = recordsListReader.ReadUInt32();
                    Console.WriteLine("");


                    // Write all record names and extensions
                    // into the new wpd file
                    using (var outWpdRecordsStream = new FileStream(outWPDfile, FileMode.Append, FileAccess.Write))
                    {
                        using (var outWpdRecordsWriter = new StreamWriter(outWpdRecordsStream))
                        {
                            outWpdRecordsWriter.Write("WPD");
                            PadNullBytes(outWpdRecordsWriter, 13);


                            uint recordsListReadPos = 4;
                            for (int r = 0; r < totalRecords; r++)
                            {
                                recordsListReader.BaseStream.Position = recordsListReadPos;
                                var currentRecordName = recordsListReader.ReadStringTillNull();
                                recordsListReadPos = (uint)recordsListReader.BaseStream.Position;

                                recordsListReader.BaseStream.Position = recordsListReadPos;
                                var currentRecordExtn = recordsListReader.ReadStringTillNull();
                                recordsListReadPos = (uint)recordsListReader.BaseStream.Position;

                                outWpdRecordsWriter.Write(currentRecordName);
                                uint bytesToPad = 16 - (uint)currentRecordName.Length;
                                PadNullBytes(outWpdRecordsWriter, bytesToPad + 8);

                                if (!currentRecordExtn.Equals("."))
                                {
                                    outWpdRecordsWriter.Write(currentRecordExtn);
                                    uint bytesToPad2 = 8 - (uint)currentRecordExtn.Length;
                                    PadNullBytes(outWpdRecordsWriter, bytesToPad2);
                                }
                                else
                                {
                                    PadNullBytes(outWpdRecordsWriter, 8);
                                }
                            }
                        }
                    }


                    uint recordDataStartPos = 0;

                    using (var outWPDdataStream = new FileStream(outWPDfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (var outWPDoffsetStream = new FileStream(outWPDfile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            using (var outWPDoffsetReader = new BinaryReader(outWPDoffsetStream))
                            {
                                using (var outWPDoffsetWriter = new BinaryWriter(outWPDoffsetStream))
                                {
                                    outWPDoffsetWriter.BaseStream.Position = 4;
                                    outWPDoffsetWriter.WriteBytesUInt32(totalRecords, true);


                                    uint readStartPos = 16;
                                    uint writeStartPos = 32;
                                    for (int o = 0; o < totalRecords; o++)
                                    {
                                        outWPDoffsetReader.BaseStream.Position = readStartPos;
                                        var currentRecordName = outWPDoffsetReader.ReadStringTillNull();

                                        outWPDoffsetReader.BaseStream.Position = readStartPos + 24;
                                        var currentRecordExtn = "." + outWPDoffsetReader.ReadStringTillNull();

                                        if (currentRecordExtn.Equals("."))
                                        {
                                            currentRecordExtn = "";
                                        }

                                        recordDataStartPos = (uint)outWPDdataStream.Length;
                                        outWPDoffsetWriter.BaseStream.Position = writeStartPos;
                                        outWPDoffsetWriter.WriteBytesUInt32(recordDataStartPos, true);

                                        var currentFile = Path.Combine(inWPDExtractedDir, currentRecordName + currentRecordExtn);

                                        if (IMGBVariables.ImgHeaderBlockExtns.Contains(currentRecordExtn))
                                        {
                                            if (Directory.Exists(inWPDExtractedIMGBDir))
                                            {
                                                IMGBRepack.RepackIMGBType1(currentFile, outWPDImgbFile, inWPDExtractedIMGBDir);
                                            }
                                        }

                                        var currentFileSize = (uint)new FileInfo(currentFile).Length;

                                        outWPDoffsetWriter.BaseStream.Position = writeStartPos + 4;
                                        outWPDoffsetWriter.WriteBytesUInt32(currentFileSize, true);

                                        using (var currentFileStream = new FileStream(currentFile, FileMode.Open, FileAccess.Read))
                                        {
                                            currentFileStream.ExCopyTo(outWPDdataStream, 0, currentFileSize);
                                        }

                                        // Pad null bytes to make the next
                                        // start position divisible by a 
                                        // pad value
                                        var currentPos = outWPDdataStream.Length;
                                        var padValue = 4;
                                        if (currentPos % padValue != 0)
                                        {
                                            var remainder = currentPos % padValue;
                                            var increaseBytes = padValue - remainder;
                                            var newPos = currentPos + increaseBytes;
                                            var nullBytesAmount = newPos - currentPos;

                                            outWPDdataStream.Seek(currentPos, SeekOrigin.Begin);
                                            for (int p = 0; p < nullBytesAmount; p++)
                                            {
                                                outWPDdataStream.WriteByte(0);
                                            }
                                        }

                                        Console.WriteLine("Repacked " + currentFile);
                                        Console.WriteLine("");

                                        recordDataStartPos += currentFileSize;
                                        readStartPos += 32;
                                        writeStartPos += 32;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Finished repacking files to " + "\"" + outWPDfileName + "\"");
        }


        static void PadNullBytes(StreamWriter streamName, uint padding)
        {
            for (int b = 0; b < padding; b++)
            {
                streamName.Write("\0");
            }
        }


        static void IfFileExistsDel(string fileToDelete)
        {
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
            }
        }
    }
}