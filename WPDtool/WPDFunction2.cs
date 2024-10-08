using IMGBlibrary.Repack;
using IMGBlibrary.Support;
using System;
using System.IO;
using System.Text;

namespace WPDtool
{
    internal partial class WPD
    {
        public static void RepackWPD(string inWPDExtractedDir)
        {
            var inWPDExtractedDirRoot = Path.GetDirectoryName(inWPDExtractedDir);

            var outWPDfileName = Path.GetFileName(inWPDExtractedDir);

            if (outWPDfileName.StartsWith("_"))
            {
                outWPDfileName = outWPDfileName.Remove(0, 1);
            }

            var outWPDfile = Path.Combine(inWPDExtractedDirRoot, outWPDfileName);
            var outWPDImgbFile = Path.Combine(inWPDExtractedDirRoot, Path.GetFileNameWithoutExtension(outWPDfileName) + ".imgb");
            var inWPDExtractedIMGBDir = Path.Combine(inWPDExtractedDirRoot, "_" + Path.GetFileNameWithoutExtension(outWPDfileName) + ".imgb");

            var recordsListFile = Path.Combine(inWPDExtractedDir, SharedMethods.RecordsList);

            if (!File.Exists(recordsListFile))
            {
                SharedMethods.ErrorExit($"Error: Missing file '{SharedMethods.RecordsList}' in extracted directory. Please ensure that the wpd file is unpacked properly with this tool.");
            }

            if (Directory.Exists(inWPDExtractedIMGBDir))
            {
                if (Directory.GetFiles(inWPDExtractedIMGBDir).Length != 0)
                {
                    if (outWPDImgbFile.EndsWith("ps3.imgb") || outWPDImgbFile.EndsWith("x360.imgb"))
                    {
                        SharedMethods.ErrorExit("Error: Detected PS3 or Xbox 360 version's extracted IMGB directory. repacking is not supported for these two versions.");
                    }

                    if (!File.Exists(outWPDImgbFile))
                    {
                        SharedMethods.ErrorExit($"Error: Paired imgb file for the extracted IMGB directory, is missing.\nPlease ensure that the paired imgb file is present next to the extracted imgb directory.");
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

            var platform = IMGBEnums.Platforms.win32;

            if (outWPDfileName.EndsWith("ps3.xgr"))
            {
                platform = IMGBEnums.Platforms.ps3;
            }
            else if (outWPDfileName.EndsWith("x360.xgr"))
            {
                platform = IMGBEnums.Platforms.x360;
            }


            using (var recordListReader = new StreamReader(recordsListFile))
            {
                var isValidNum = uint.TryParse(recordListReader.ReadLine(), out uint totalRecords);

                if (!isValidNum)
                {
                    SharedMethods.ErrorExit($"Specified record count is invalid in the {SharedMethods.RecordsList} file");
                }

                Console.WriteLine("");


                // Write all record names and extensions
                // into the new wpd file
                using (var outWpdRecordsWriter = new StreamWriter(outWPDfile, true, new UTF8Encoding(false)))
                {
                    outWpdRecordsWriter.Write("WPD");
                    PadNullBytes(outWpdRecordsWriter, 13);

                    for (int r = 0; r < totalRecords; r++)
                    {
                        var currentRecordLineData = recordListReader.ReadLine().Split(SharedMethods.DataSplitChar, StringSplitOptions.None);

                        var currentRecordNameArray = Encoding.UTF8.GetBytes(currentRecordLineData[0]);

                        outWpdRecordsWriter.Write(Encoding.UTF8.GetString(currentRecordNameArray));
                        PadNullBytes(outWpdRecordsWriter, (16 - (uint)currentRecordNameArray.Length) + 8);

                        if (currentRecordLineData[1] == "null")
                        {
                            PadNullBytes(outWpdRecordsWriter, 8);
                        }
                        else
                        {
                            outWpdRecordsWriter.Write(currentRecordLineData[1]);
                            PadNullBytes(outWpdRecordsWriter, 8 - (uint)currentRecordLineData[1].Length);
                        }
                    }
                }


                // Copy in all record's data into the file
                // and update the offsets
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
                                    var currentRecordNameArray = outWPDoffsetReader.ReadBytesTillNull().ToArray();
                                    var currentRecordName = Encoding.UTF8.GetString(currentRecordNameArray);

                                    var recordNameAdjusted = SharedMethods.RemoveIllegalChars(currentRecordName);

                                    outWPDoffsetReader.BaseStream.Position = readStartPos + 24;
                                    var currentRecordExtn = "." + outWPDoffsetReader.ReadStringTillNull();

                                    if (currentRecordExtn.Equals("."))
                                    {
                                        currentRecordExtn = "";
                                    }

                                    recordDataStartPos = (uint)outWPDdataStream.Length;
                                    outWPDoffsetWriter.BaseStream.Position = writeStartPos;
                                    outWPDoffsetWriter.WriteBytesUInt32(recordDataStartPos, true);

                                    var currentFile = Path.Combine(inWPDExtractedDir, recordNameAdjusted + currentRecordExtn);

                                    if (Enum.TryParse(currentRecordExtn.Replace(".", ""), false, out IMGBEnums.FileExtensions fileExtension) == true)
                                    {
                                        if (Directory.Exists(inWPDExtractedIMGBDir))
                                        {
                                            IMGBRepack1.RepackIMGBType1(currentFile, outWPDImgbFile, inWPDExtractedIMGBDir, platform, true);
                                        }
                                    }

                                    var currentFileSize = (uint)new FileInfo(currentFile).Length;

                                    outWPDoffsetWriter.BaseStream.Position = writeStartPos + 4;
                                    outWPDoffsetWriter.WriteBytesUInt32(currentFileSize, true);

                                    using (var currentFileStream = new FileStream(currentFile, FileMode.Open, FileAccess.Read))
                                    {
                                        currentFileStream.Position = 0;
                                        currentFileStream.CopyStreamTo(outWPDdataStream, currentFileSize, false);
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
                                        outWPDdataStream.PadNull((int)nullBytesAmount);
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

            Console.WriteLine("");
            Console.WriteLine("Finished repacking record files to " + "\"" + outWPDfileName + "\"");
        }


        private static void PadNullBytes(StreamWriter streamName, uint padding)
        {
            for (int b = 0; b < padding; b++)
            {
                streamName.Write("\0");
            }
        }


        private static void IfFileExistsDel(string fileToDelete)
        {
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
            }
        }
    }
}