using System;
using System.IO;
using WPDtool.SupportClasses;
using WPDtool.WPDClasses;

namespace WPDtool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                CmnMethods.ErrorExit("Error: Enough arguments not specified\nMust be: WPDtool.exe '-u' or '-r' and 'WPD file or unpacked WPD folder'");
            }

            var actionEnumString = args[0].Replace("-", "");

            var convertedAction = new CmnMethods.ActionEnums();
            if (Enum.TryParse(actionEnumString, false, out CmnMethods.ActionEnums resultEnum))
            {
                convertedAction = resultEnum;
            }

            var inWPDfileOrFolder = args[1];

            switch (convertedAction)
            {
                case CmnMethods.ActionEnums.u:
                    if (!File.Exists(inWPDfileOrFolder))
                    {
                        CmnMethods.ErrorExit("Error: Specified file does not exist");
                    }
                    WPD.UnpackWPD(inWPDfileOrFolder);
                    break;

                case CmnMethods.ActionEnums.r:
                    if (!Directory.Exists(inWPDfileOrFolder))
                    {
                        CmnMethods.ErrorExit("Error: Specified directory to repack, does not exist");
                    }
                    WPD.RepackWPD(inWPDfileOrFolder);
                    break;

                default:
                    CmnMethods.ErrorExit("Error: Proper tool action is not specified.\nMust be: '-u', '-r' or '-rb'");
                    break;
            }
        }
    }
}