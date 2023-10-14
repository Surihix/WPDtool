using System;
using System.IO;

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