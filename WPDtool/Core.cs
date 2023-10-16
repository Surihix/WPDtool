﻿using System;
using System.IO;
using System.Security.Cryptography;

namespace WPDtool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                CmnMethods.ErrorExit("Error: Enough arguments not specified\nMust be: WPDtool.exe '-u' or '-r' and 'WPD file or unpacked WPD folder'.");
            }

            var toolAction = args[0].Replace("-", "");
            var inWPDfileOrDir = args[1];


            // Dll check
            if (File.Exists("IMGBlibrary.dll"))
            {
                using (var dllStream = new FileStream("IMGBlibrary.dll", FileMode.Open, FileAccess.Read))
                {
                    using (var dllHash = SHA256.Create())
                    {
                        var hashArray = dllHash.ComputeHash(dllStream);
                        var computedHash = BitConverter.ToString(hashArray).Replace("-", "").ToLower();

                        if (!computedHash.Equals("259d66e27a0ec1909d300f7383fc1ba2866dcba2ed1a73293ccd9307f65137d8"))
                        {
                            CmnMethods.ErrorExit("Error: 'IMGBlibrary.dll' file is corrupt. please check if the dll file is valid.");
                        }
                    }
                }
            }
            else
            {
                CmnMethods.ErrorExit("Error: Missing 'IMGBlibrary.dll' file. please ensure that the dll file exists next to the program.");
            }


            try
            {
                var convertedToolAction = new ActionSwitches();
                if (Enum.TryParse(toolAction, false, out ActionSwitches convertedActionSwitch))
                {
                    convertedToolAction = convertedActionSwitch;
                }
                else
                {
                    CmnMethods.ErrorExit("Error: Proper tool action is not specified\nMust be '-u' for unpacking or '-r' for repacking.");
                }

                switch (convertedToolAction)
                {
                    case ActionSwitches.u:
                        if (!File.Exists(inWPDfileOrDir))
                        {
                            CmnMethods.ErrorExit("Error: Specified WPD file does not exist.");
                        }
                        WPD.UnpackWPD(inWPDfileOrDir);
                        break;

                    case ActionSwitches.r:
                        if (!Directory.Exists(inWPDfileOrDir))
                        {
                            CmnMethods.ErrorExit("Error: Specified unpacked directory to repack, does not exist.");
                        }
                        WPD.RepackWPD(inWPDfileOrDir);
                        break;
                }
            }
            catch (Exception ex)
            {
                CmnMethods.ErrorExit("" + ex);
            }
        }

        public enum ActionSwitches
        {
            u,
            r
        }
    }
}