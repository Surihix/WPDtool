using System;
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
                WPDMethods.ErrorExit("Error: Enough arguments not specified\n" +
                    "\nFor Unpacking: WPDtool.exe -u \"WPD file\" " +
                    "\nFor Repacking: WPDtool.exe -r \"unpacked WPD folder\"");
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

                        if (!computedHash.Equals("7201f9319a94a3d8cb618e1a8379af1324e0b9433f6a286cb590718e376ef55e"))
                        {
                            WPDMethods.ErrorExit("Error: 'IMGBlibrary.dll' file is corrupt. please check if the dll file is valid.");
                        }
                    }
                }
            }
            else
            {
                WPDMethods.ErrorExit("Error: Missing 'IMGBlibrary.dll' file. please ensure that the dll file exists next to the program.");
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
                    WPDMethods.ErrorExit("Error: Proper tool action is not specified\nMust be '-u' for unpacking or '-r' for repacking.");
                }

                switch (convertedToolAction)
                {
                    case ActionSwitches.u:
                        if (!File.Exists(inWPDfileOrDir))
                        {
                            WPDMethods.ErrorExit("Error: Specified WPD file does not exist.");
                        }
                        WPD.UnpackWPD(inWPDfileOrDir);
                        break;

                    case ActionSwitches.r:
                        if (!Directory.Exists(inWPDfileOrDir))
                        {
                            WPDMethods.ErrorExit("Error: Specified unpacked directory to repack, does not exist.");
                        }
                        WPD.RepackWPD(inWPDfileOrDir);
                        break;
                }
            }
            catch (Exception ex)
            {
                WPDMethods.ErrorExit("" + ex);
            }
        }

        enum ActionSwitches
        {
            u,
            r
        }
    }
}